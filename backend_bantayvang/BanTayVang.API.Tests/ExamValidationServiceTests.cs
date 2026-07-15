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
        private readonly Mock<IExamPaperRepository> _mockDethiRepository;
        private readonly Mock<IQuestionRepository> _mockCauhoiRepository;
        private readonly ExamValidationService _validationService;

        public ExamValidationServiceTests()
        {
            _mockDethiRepository = new Mock<IExamPaperRepository>();
            _mockCauhoiRepository = new Mock<IQuestionRepository>();
            var mockBaithiRepository = new Mock<IExamSubmissionRepository>();
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
            var createDto = new CreateExamPaperDto
            {
                ExamPaperCode = "examPaper-001", // Lowercase and hyphen
                ExamPaperName = "Test Exam",
                DurationMinutes = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamPaper?)null); // No duplicate

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
            var createDto = new CreateExamPaperDto
            {
                ExamPaperCode = "DETHI_002", // Uppercase and underscore
                ExamPaperName = "Test Exam",
                DurationMinutes = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamPaper?)null); // No duplicate

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
            var createDto = new CreateExamPaperDto
            {
                ExamPaperCode = "examPaper 003", // Invalid because of space
                ExamPaperName = "Test Exam",
                DurationMinutes = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamPaper?)null);

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
            var createDto = new CreateExamPaperDto
            {
                ExamPaperCode = "DETHI_004",
                ExamPaperName = "Test Exam",
                DurationMinutes = 60,
                DanhSachIdCauHoi = new List<int>()
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExamPaper()); // Returns an existing exam => Duplicate!

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
            var createDto = new CreateExamPaperDto
            {
                ExamPaperCode = "DETHI_005",
                ExamPaperName = "Test Exam",
                DurationMinutes = 60,
                DanhSachIdCauHoi = new List<int> { 1, 2, 3 } // Needs 1, 2, 3
            };

            _mockDethiRepository.Setup(r => r.GetByMaDeThiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamPaper?)null);

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
