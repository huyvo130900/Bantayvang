using ClosedXML.Excel;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Import;

namespace BanTayVang.API.Services.Impl.Import
{
    public class MultipleChoiceImportStrategy : IQuestionImportStrategy
    {
        private readonly ICauhoiRepository _cauhoiRepository;

        public MultipleChoiceImportStrategy(ICauhoiRepository cauhoiRepository)
        {
            _cauhoiRepository = cauhoiRepository;
        }

        public string QuestionTypeName => "Trắc nghiệm";

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

                    // 4. Đọc các lựa chọn A, B, C, D (Cột 2 -> 5)
                    var choices = new List<Luachon>();
                    for (int i = 0; i < 4; i++)
                    {
                        var choiceContent = row.Cell(2 + i).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(choiceContent))
                        {
                            choices.Add(new Luachon 
                            { 
                                NoiDung = choiceContent, 
                                ThuTu = i + 1, 
                                LaDapAnDung = false 
                            });
                        }
                    }

                    if (choices.Count < 2)
                    {
                        errors.Add($"Dòng {rowNumber}: Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn.");
                        continue;
                    }

                    // 5. Đáp án đúng (Cột 6) - không phân biệt hoa thường, chấp nhận 'a', 'b', 'c', 'd' hoặc '1', '2', '3', '4'
                    var correctValStr = row.Cell(6).GetString().Trim().ToLowerInvariant();
                    int correctIndex = -1;
                    if (correctValStr == "a" || correctValStr == "1") correctIndex = 1;
                    else if (correctValStr == "b" || correctValStr == "2") correctIndex = 2;
                    else if (correctValStr == "c" || correctValStr == "3") correctIndex = 3;
                    else if (correctValStr == "d" || correctValStr == "4") correctIndex = 4;

                    if (correctIndex < 1 || correctIndex > choices.Count)
                    {
                        errors.Add($"Dòng {rowNumber}: Đáp án đúng không hợp lệ. Vui lòng nhập A, B, C, hoặc D.");
                        continue;
                    }
                    choices[correctIndex - 1].LaDapAnDung = true;

                    // 5.5 Độ khó (Cột 7)
                    string doKho = "1"; // Mặc định là Dễ (1)
                    if (!isExamImport)
                    {
                        var doKhoValStr = row.Cell(7).GetString().Trim().ToLowerInvariant();
                        if (doKhoValStr == "3" || doKhoValStr == "k" || doKhoValStr.Contains("khó") || doKhoValStr.Contains("kho")) doKho = "3";
                        else if (doKhoValStr == "2" || doKhoValStr == "tb" || doKhoValStr.Contains("trung bình") || doKhoValStr.Contains("trung binh")) doKho = "2";
                        else if (doKhoValStr == "1" || doKhoValStr.Contains("dễ") || doKhoValStr.Contains("de")) doKho = "1";
                    }

                    // 6. Kiểm tra trùng lặp
                    var noiDungChuan = noiDung.ToLower();
                    
                    // Kiểm tra trùng lặp trong cùng file
                    if (!seenContents.Add(noiDungChuan))
                    {
                        errors.Add($"Dòng {rowNumber}: Câu hỏi trùng lặp trong cùng file Excel — \"{noiDung}\" — bỏ qua.");
                        continue;
                    }

                    // Nếu không phải import cho đề thi, mới kiểm tra trùng lặp với CSDL
                    if (!isExamImport)
                    {
                        var existingQuestion = await _cauhoiRepository.FindDuplicateAsync(noiDungChuan, khoaPhong);
                        if (existingQuestion != null)
                        {
                            errors.Add($"Dòng {rowNumber}: Câu hỏi đã tồn tại (Id: {existingQuestion.Id}) — \"{noiDung}\" — bỏ qua.");
                            continue;
                        }
                    }

                    // 7. Tạo thực thể câu hỏi
                    var cauhoi = new Cauhoi
                    {
                        NoiDung = noiDung,
                        DoKho = doKho,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.Now,
                        DaXoa = false,
                        KhoaPhong = khoaPhong,
                        Luachons = choices
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
                ? new[] 
                { 
                    "Nội dung câu hỏi (*)", 
                    "Đáp án A (*)", 
                    "Đáp án B (*)", 
                    "Đáp án C", 
                    "Đáp án D", 
                    "Đáp án đúng (*)\n(A, B, C hoặc D)"
                }
                : new[] 
                { 
                    "Nội dung câu hỏi (*)", 
                    "Đáp án A (*)", 
                    "Đáp án B (*)", 
                    "Đáp án C", 
                    "Đáp án D", 
                    "Đáp án đúng (*)\n(A, B, C hoặc D)",
                    "Độ khó (*)\n(1: Dễ, 2: TB, 3: Khó)"
                };
            var widths = isExamImport
                ? new[] { 55, 30, 30, 30, 30, 18 }
                : new[] { 55, 30, 30, 30, 30, 18, 22 };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E4057");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Column(c + 1).Width = widths[c];
            }
            worksheet.Row(1).Height = 42;

            // Dòng dữ liệu mẫu
            var sampleRow = isExamImport
                ? new object[] 
                { 
                    "Kỹ thuật rửa tay đúng cách theo WHO gồm mấy bước?", 
                    "3 bước", 
                    "5 bước", 
                    "6 bước", 
                    "7 bước", 
                    "b"
                }
                : new object[] 
                { 
                    "Kỹ thuật rửa tay đúng cách theo WHO gồm mấy bước?", 
                    "3 bước", 
                    "5 bước", 
                    "6 bước", 
                    "7 bước", 
                    "b",
                    "1"
                };

            for (int c = 0; c < sampleRow.Length; c++)
            {
                var cell = worksheet.Cell(2, c + 1);
                if (sampleRow[c] is double d) cell.Value = d;
                else if (sampleRow[c] is int i) cell.Value = i;
                else cell.Value = sampleRow[c]?.ToString() ?? "";
                
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.WrapText = true;
            }

            worksheet.SheetView.FreezeRows(1);
        }
    }
}
