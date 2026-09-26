using BanTayVang.API.Models;
using Microsoft.AspNetCore.Http;

namespace BanTayVang.API.Services.Interfaces.Import
{
    /// <summary>
    /// Service xử lý import câu hỏi từ file Word (.docx).
    /// Cú pháp chuẩn Azota: Bôi đậm (Bold) hoặc gạch chân (Underline)
    /// ký tự A, B, C, D để đánh dấu đáp án đúng.
    /// </summary>
    public interface IWordQuestionImportService
    {
        /// <summary>
        /// Đọc, bóc tách và validate câu hỏi từ file Word.
        /// </summary>
        /// <param name="file">File .docx upload lên từ Frontend.</param>
        /// <param name="createdBy">UserId của người thực hiện Import.</param>
        /// <param name="department">Khoa/phòng gán cho câu hỏi.</param>
        /// <param name="questionCategoryId">ID loại câu hỏi (Trắc nghiệm / Tự luận).</param>
        /// <param name="errors">Danh sách lỗi được ghi ra nếu có.</param>
        /// <returns>Danh sách Question (kèm QuestionOptions) đã qua validate, sẵn sàng để lưu DB.</returns>
        Task<List<Question>> ParseAndValidateAsync(
            IFormFile file,
            int createdBy,
            string department,
            int questionCategoryId,
            List<string> errors);

        /// <summary>
        /// Sinh file Word mẫu (.docx) để người dùng tải về và điền câu hỏi theo chuẩn.
        /// </summary>
        /// <returns>Nội dung file Word dưới dạng mảng byte.</returns>
        byte[] GenerateTemplateDocx();
    }
}
