using ClosedXML.Excel;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Import;

namespace BanTayVang.API.Services.Impl.Import
{
    public class EssayImportStrategy : IQuestionImportStrategy
    {
        private readonly ICauhoiRepository _cauhoiRepository;

        public EssayImportStrategy(ICauhoiRepository cauhoiRepository)
        {
            _cauhoiRepository = cauhoiRepository;
        }

        public string QuestionTypeName => "Tự luận";

        public async Task<List<Cauhoi>> ParseAndValidateAsync(
            IXLWorksheet worksheet, 
            int nguoiTao, 
            string khoaPhong, 
            List<string> errors,
            bool isExamImport = false)
        {
            var parsedQuestions = new List<Cauhoi>();
            var rows = worksheet.RowsUsed().Skip(1); // Bỏ qua header
            int rowNumber = 1;
            var seenContents = new HashSet<string>();

            foreach (var row in rows)
            {
                rowNumber++;
                try
                {
                    // 1. Nội dung câu hỏi (Cột 1)
                    var noiDung = row.Cell(1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(noiDung))
                        continue;

                    // 2. Độ khó (Cột 2)
                    string doKho = "1"; // Mặc định là Dễ (1)
                    if (!isExamImport)
                    {
                        var doKhoValStr = row.Cell(2).GetString().Trim().ToLowerInvariant();
                        if (doKhoValStr == "3" || doKhoValStr == "k" || doKhoValStr.Contains("khó") || doKhoValStr.Contains("kho")) doKho = "3";
                        else if (doKhoValStr == "2" || doKhoValStr == "tb" || doKhoValStr.Contains("trung bình") || doKhoValStr.Contains("trung binh")) doKho = "2";
                        else if (doKhoValStr == "1" || doKhoValStr.Contains("dễ") || doKhoValStr.Contains("de")) doKho = "1";
                    }

                    // 3. Kiểm tra trùng lặp
                    var noiDungChuan = noiDung.ToLower();
                    
                    // Kiểm tra trùng lặp trong cùng file
                    if (!seenContents.Add(noiDungChuan))
                    {
                        errors.Add($"Dòng {rowNumber}: Câu hỏi trùng lặp trong cùng file Excel — \"{noiDung}\" — bỏ qua.");
                        continue;
                    }

                    // Kiểm tra trùng lặp với CSDL (chỉ kiểm tra nếu không phải "Không thuộc ngân hàng")
                    if (khoaPhong != "Không thuộc ngân hàng")
                    {
                        var existingQuestion = await _cauhoiRepository.FindDuplicateAsync(noiDungChuan, khoaPhong);
                        if (existingQuestion != null)
                        {
                            errors.Add($"Dòng {rowNumber}: Câu hỏi tự luận đã tồn tại trong CSDL (Id: {existingQuestion.Id}) — \"{noiDung}\"");
                            continue;
                        }
                    }

                    // 4. Tạo thực thể câu hỏi (Không có lựa chọn)
                    var cauhoi = new Cauhoi
                    {
                        NoiDung = noiDung,
                        DoKho = doKho,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.Now,
                        DaXoa = false,
                        KhoaPhong = khoaPhong,
                        Luachons = new List<Luachon>() // Không có lựa chọn cho tự luận
                    };

                    parsedQuestions.Add(cauhoi);
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {rowNumber}: {ex.Message}");
                }
            }

            return parsedQuestions;
        }

        public void GenerateTemplate(IXLWorksheet worksheet, bool isExamImport = false)
        {
            // Thiết lập tiêu đề cột
            var headers = isExamImport
                ? new[] { "Nội dung câu hỏi" }
                : new[] { "Nội dung câu hỏi", "Độ khó" };
            var widths = isExamImport
                ? new[] { 80 }
                : new[] { 65, 15 };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#375623"); // Màu xanh lá sậm cho tự luận
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Column(c + 1).Width = widths[c];
            }
            worksheet.Row(1).Height = 35;

            // Dòng dữ liệu mẫu
            var sampleRow = isExamImport
                ? new object[] { "Trình bày kỹ thuật rửa tay ngoại khoa theo hướng dẫn của Bộ Y tế?" }
                : new object[] { "Trình bày kỹ thuật rửa tay ngoại khoa theo hướng dẫn của Bộ Y tế?", "2" };

            for (int c = 0; c < sampleRow.Length; c++)
            {
                var cell = worksheet.Cell(2, c + 1);
                if (sampleRow[c] is double d) cell.Value = d;
                else cell.Value = sampleRow[c]?.ToString() ?? "";
                
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.WrapText = true;
            }

            worksheet.SheetView.FreezeRows(1);
        }
    }
}
