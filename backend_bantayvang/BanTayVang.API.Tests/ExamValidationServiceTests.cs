using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Impl.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BanTayVang.API.Tests
{
    public class ExamValidationServiceTests
    {
        private readonly Mock<IDethiRepository> _mockDethiRepository;
        private readonly Mock<ICauhoiRepository> _mockCauhoiRepository;
        private readonly ExamValidationService _validationService;

        public ExamValidationServiceTests()
        {
            _mockDethiRepository = new Mock<IDethiRepository>();
            _mockCauhoiRepository = new Mock<ICauhoiRepository>();
            var mockBaithiRepository = new Mock<IBaithiRepository>();
            var mockLogger = new Mock<ILogger<ExamValidationService>>();

            _validationService = new ExamValidationService(
                _mockDethiRepository.Object,
                mockBaithiRepository.Object,
                _mockCauhoiRepository.Object,
                null!, // _context is not used in ValidateCreateExamAsync
                mockLogger.Object
            );
        }

        [Fact]
        public async Task ValidateCreateExamAsync_ValidLowercaseExamCode_ReturnsSuccess()
        {
            // Arrange
            var createDto = new CreateDethiDto
            {
                MaDeThi = "dethi-001", // Lowercase and hyphen
                TenDeThi = "Test Exam",
                ThoiGianLamBai = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Dethi?)null); // No duplicate

            // Act
            var result = await _validationService.ValidateCreateExamAsync(createDto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateExamAsync_ValidUppercaseExamCode_ReturnsSuccess()
        {
            // Arrange
            var createDto = new CreateDethiDto
            {
                MaDeThi = "DETHI_002", // Uppercase and underscore
                TenDeThi = "Test Exam",
                ThoiGianLamBai = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Dethi?)null); // No duplicate

            // Act
            var result = await _validationService.ValidateCreateExamAsync(createDto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateExamAsync_InvalidExamCodeFormat_ReturnsError()
        {
            // Arrange
            var createDto = new CreateDethiDto
            {
                MaDeThi = "dethi 003", // Invalid because of space
                TenDeThi = "Test Exam",
                ThoiGianLamBai = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Dethi?)null);

            // Act
            var result = await _validationService.ValidateCreateExamAsync(createDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Exam code format is invalid", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateExamAsync_DuplicateExamCode_ReturnsError()
        {
            // Arrange
            var createDto = new CreateDethiDto
            {
                MaDeThi = "DETHI_004",
                TenDeThi = "Test Exam",
                ThoiGianLamBai = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dethi()); // Returns an existing exam => Duplicate!

            // Act
            var result = await _validationService.ValidateCreateExamAsync(createDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Exam code already exists", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateExamAsync_InvalidQuestionIds_ReturnsError()
        {
            // Arrange
            var createDto = new CreateDethiDto
            {
                MaDeThi = "DETHI_005",
                TenDeThi = "Test Exam",
                ThoiGianLamBai = 60,
                DanhSachIdCauHoi = new List<int> { 1, 2, 3 } // Needs 1, 2, 3
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Dethi?)null);

            // Mock repository returns only 1 and 2, missing 3
            _mockCauhoiRepository.Setup(r => r.GetValidQuestionIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 1, 2 });

            // Act
            var result = await _validationService.ValidateCreateExamAsync(createDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.StartsWith("Invalid question IDs: 3"));
        }
    }
}
