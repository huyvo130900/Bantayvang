using BanTayVang.API.Services.Interfaces.Import;

namespace BanTayVang.API.Services.Impl.Import
{
    public class QuestionImportStrategyFactory : IQuestionImportStrategyFactory
    {
        private readonly IEnumerable<IQuestionImportStrategy> _strategies;

        public QuestionImportStrategyFactory(IEnumerable<IQuestionImportStrategy> strategies)
        {
            _strategies = strategies;
        }

        /// <summary>
        /// Select strategy by question type ID.
        /// Kept for backward compatibility only — prefer GetStrategy(categoryName) which is ID-independent.
        /// </summary>
        [Obsolete("Use GetStrategy(categoryName) to avoid ID coupling with DB.")]
        public IQuestionImportStrategy GetStrategyById(int questionTypeId)
        {
            return questionTypeId switch
            {
                1 => _strategies.FirstOrDefault(s => s is MultipleChoiceImportStrategy)
                     ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Trắc nghiệm."),
                2 => _strategies.FirstOrDefault(s => s is EssayImportStrategy)
                     ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Tự luận."),
                _ => throw new NotSupportedException($"Loại câu hỏi ID '{questionTypeId}' không được hỗ trợ import qua Excel. Thay vào đó hãy dùng GetStrategy(categoryName).")
            };
        }

        public IQuestionImportStrategy GetStrategy(string questionTypeName)
        {
            if (string.IsNullOrWhiteSpace(questionTypeName))
            {
                throw new ArgumentException("Tên loại câu hỏi không được để trống.", nameof(questionTypeName));
            }

            var normalizedName = questionTypeName.Trim().ToLower();

            // Khớp cả tên đầy đủ lẫn viết tắt từ DB (TN / TL)
            bool isMultipleChoice = normalizedName == "tn"
                || normalizedName.Contains("trắc nghiệm") || normalizedName.Contains("trac nghiem")
                || normalizedName.Contains("tr?c nghi?m") || normalizedName.Contains("multiple");

            bool isEssay = normalizedName == "tl"
                || normalizedName.Contains("tự luận") || normalizedName.Contains("tu luan")
                || normalizedName.Contains("t? lu?n") || normalizedName.Contains("essay");

            if (isMultipleChoice)
            {
                return _strategies.FirstOrDefault(s => s is MultipleChoiceImportStrategy)
                    ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Trắc nghiệm.");
            }

            if (isEssay)
            {
                return _strategies.FirstOrDefault(s => s is EssayImportStrategy)
                    ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Tự luận.");
            }

            throw new NotSupportedException($"Loại câu hỏi '{questionTypeName}' không được hỗ trợ import qua Excel. Các giá trị hợp lệ: TN, TL, Trắc nghiệm, Tự luận.");
        }
    }
}
