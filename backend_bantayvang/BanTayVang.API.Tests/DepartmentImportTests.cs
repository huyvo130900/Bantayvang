using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BanTayVang.API.Controllers;
using BanTayVang.API.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BanTayVang.API.Tests
{
    public class DepartmentImportTests : IDisposable
    {
        private readonly BanTayVangDbContext _context;
        private readonly DepartmentController _controller;

        public DepartmentImportTests()
        {
            var options = new DbContextOptionsBuilder<BanTayVangDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BanTayVangDbContext(options);
            _context.Database.EnsureCreated();

            var mockLogger = new Mock<ILogger<DepartmentController>>();
            _controller = new DepartmentController(_context, mockLogger.Object);
        }

        private IFormFile CreateMockExcelFile(string fileName, string[] headers, List<string[]> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("IMPORT_KHOA_PHONG");

            // Write headers
            for (int col = 1; col <= headers.Length; col++)
            {
                ws.Cell(1, col).Value = headers[col - 1];
            }

            // Write rows
            for (int r = 0; r < rows.Count; r++)
            {
                var rowData = rows[r];
                for (int col = 1; col <= rowData.Length; col++)
                {
                    ws.Cell(r + 2, col).Value = rowData[col - 1];
                }
            }

            var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var content = ms.ToArray();

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(content.Length);

            return fileMock.Object;
        }

        [Fact]
        public async Task Import_WithSTTAndKhoaPhongColumns_ImportsSuccessfullyAndGeneratesUniqueMaKhoa()
        {
            // Arrange
            var headers = new[] { "STT", "Khoa/ phòng" };
            var rows = new List<string[]>
            {
                new[] { "1", "BAN GIÁM ĐỐC" },
                new[] { "2", "Phòng Tổ chức cán bộ" },
                new[] { "3", "Phòng Điều dưỡng" }
            };
            var file = CreateMockExcelFile("khoaphong_stt.xlsx", headers, rows);

            // Act
            var result = await _controller.ImportDepartments(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            Assert.True(data.GetProperty("success").GetBoolean());
            Assert.Equal(3, data.GetProperty("created").GetInt32());

            var dbDepts = await _context.Departments.ToListAsync();
            Assert.Equal(3, dbDepts.Count);

            // Verify generated DeptCode (clean, uppercase, underscore, max 50 chars)
            var bgd = dbDepts.FirstOrDefault(d => d.DepartmentName == "BAN GIÁM ĐỐC");
            Assert.NotNull(bgd);
            Assert.Equal("BAN_GIAM_DOC", bgd.DeptCode);

            var tccb = dbDepts.FirstOrDefault(d => d.DepartmentName == "Phòng Tổ chức cán bộ");
            Assert.NotNull(tccb);
            Assert.Equal("PHONG_TO_CHUC_CAN_BO", tccb.DeptCode);
        }

        [Fact]
        public async Task Import_WithOnlyKhoaPhongColumn_ImportsSuccessfully()
        {
            // Arrange
            var headers = new[] { "Khoa/ phòng" };
            var rows = new List<string[]>
            {
                new[] { "Khoa Cấp cứu" },
                new[] { "Khoa Ngoại tổng hợp" }
            };
            var file = CreateMockExcelFile("khoaphong_only.xlsx", headers, rows);

            // Act
            var result = await _controller.ImportDepartments(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            Assert.True(data.GetProperty("success").GetBoolean());
            Assert.Equal(2, data.GetProperty("created").GetInt32());

            var dbDepts = await _context.Departments.ToListAsync();
            Assert.Equal(2, dbDepts.Count);

            var cc = dbDepts.FirstOrDefault(d => d.DepartmentName == "Khoa Cấp cứu");
            Assert.NotNull(cc);
            Assert.Equal("KHOA_CAP_CUU", cc.DeptCode);
        }

        [Fact]
        public async Task Import_WithStandardTemplate_ImportsSuccessfully()
        {
            // Arrange
            var headers = new[] { "Mã Khoa (*)", "Tên Khoa (*)", "Trạng thái (HoatDong/TamDung)", "Mô tả" };
            var rows = new List<string[]>
            {
                new[] { "KHOA_NOI", "Khoa Nội", "HoatDong", "Khoa nội tổng quát" },
                new[] { "KHOA_NHI", "Khoa Nhi", "HoatDong", "Khoa nhi tổng quát" }
            };
            var file = CreateMockExcelFile("standard_template.xlsx", headers, rows);

            // Act
            var result = await _controller.ImportDepartments(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            Assert.True(data.GetProperty("success").GetBoolean());
            Assert.Equal(2, data.GetProperty("created").GetInt32());

            var dbDepts = await _context.Departments.ToListAsync();
            Assert.Equal(2, dbDepts.Count);

            var kn = dbDepts.FirstOrDefault(d => d.DeptCode == "KHOA_NOI");
            Assert.NotNull(kn);
            Assert.Equal("Khoa Nội", kn.DepartmentName);
            Assert.True(kn.Status);
            Assert.Equal("Khoa nội tổng quát", kn.Description);
        }

        [Fact]
        public async Task Import_WithDuplicateTenKhoa_SkipsDuplicates()
        {
            // Arrange
            // Seed a department first
            _context.Departments.Add(new Department { DeptCode = "BAN_GIAM_DOC", DepartmentName = "BAN GIÁM ĐỐC", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            var headers = new[] { "Khoa/ phòng" };
            var rows = new List<string[]>
            {
                new[] { "BAN GIÁM ĐỐC" },
                new[] { "Khoa Cấp cứu" }
            };
            var file = CreateMockExcelFile("duplicates.xlsx", headers, rows);

            // Act
            var result = await _controller.ImportDepartments(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            Assert.True(data.GetProperty("success").GetBoolean());
            Assert.Equal(1, data.GetProperty("created").GetInt32()); // Only Khoa Cấp cứu created
            Assert.Equal(1, data.GetProperty("skipped").GetInt32()); // BAN GIÁM ĐỐC skipped

            var dbDepts = await _context.Departments.ToListAsync();
            Assert.Equal(2, dbDepts.Count); // 1 pre-existing + 1 imported
        }

        private IFormFile CreateMockCsvFile(string fileName, string[] headers, List<string[]> rows)
        {
            var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));
                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(",", row.Select(r => $"\"{r.Replace("\"", "\"\"")}\"")));
                }
            }
            ms.Position = 0;

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);

            return fileMock.Object;
        }

        [Fact]
        public async Task Import_WithCsvFile_ImportsSuccessfully()
        {
            // Arrange
            var headers = new[] { "Mã Khoa", "Tên Khoa", "Trạng thái", "Mô tả" };
            var rows = new List<string[]>
            {
                new[] { "KHOA_TIM_MACH", "Khoa Tim Mạch", "Hoạt động", "Chẩn đoán tim mạch" },
                new[] { "KHOA_THAN_KINH", "Khoa Thần Kinh", "Tạm dừng", "Chẩn đoán thần kinh" }
            };
            var file = CreateMockCsvFile("departments.csv", headers, rows);

            // Act
            var result = await _controller.ImportDepartments(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            Assert.True(data.GetProperty("success").GetBoolean());
            Assert.Equal(2, data.GetProperty("created").GetInt32());

            var dbDepts = await _context.Departments.ToListAsync();
            Assert.Equal(2, dbDepts.Count);

            var tm = dbDepts.FirstOrDefault(d => d.DeptCode == "KHOA_TIM_MACH");
            Assert.NotNull(tm);
            Assert.Equal("Khoa Tim Mạch", tm.DepartmentName);
            Assert.True(tm.Status);

            var tk = dbDepts.FirstOrDefault(d => d.DeptCode == "KHOA_THAN_KINH");
            Assert.NotNull(tk);
            Assert.Equal("Khoa Thần Kinh", tk.DepartmentName);
            Assert.False(tk.Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
