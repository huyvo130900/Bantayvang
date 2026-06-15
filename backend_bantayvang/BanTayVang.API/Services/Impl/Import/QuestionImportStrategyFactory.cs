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
        /// Select strategy by question type ID (preferred - avoids encoding issues with Vietnamese names).
        /// ID 1 = Trắc nghiệm (MultipleChoice), ID 2 = Tự luận (Essay).
        /// </summary>
        public IQuestionImportStrategy GetStrategyById(int questionTypeId)
        {
            return questionTypeId switch
            {
                1 => _strategies.FirstOrDefault(s => s is MultipleChoiceImportStrategy)
                     ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Trắc nghiệm."),
                3 => _strategies.FirstOrDefault(s => s is EssayImportStrategy)
                     ?? throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Tự luận."),
                _ => throw new NotSupportedException($"Loại câu hỏi ID '{questionTypeId}' không được hỗ trợ import qua Excel.")
            };
        }

        public IQuestionImportStrategy GetStrategy(string questionTypeName)
        {
            if (string.IsNullOrWhiteSpace(questionTypeName))
            {
                throw new ArgumentException("Tên loại câu hỏi không được để trống.", nameof(questionTypeName));
            }

            var normalizedName = questionTypeName.Trim().ToLower();

            if (normalizedName.Contains("trắc nghiệm") || normalizedName.Contains("trac nghiem")
                || normalizedName.Contains("tr?c nghi?m"))
            {
                var strategy = _strategies.FirstOrDefault(s => s is MultipleChoiceImportStrategy);
                if (strategy == null)
                    throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Trắc nghiệm.");
                return strategy;
            }

            if (normalizedName.Contains("tự luận") || normalizedName.Contains("tu luan")
                || normalizedName.Contains("t? lu?n"))
            {
                var strategy = _strategies.FirstOrDefault(s => s is EssayImportStrategy);
                if (strategy == null)
                    throw new InvalidOperationException("Không tìm thấy chiến lược import câu hỏi Tự luận.");
                return strategy;
            }

            throw new NotSupportedException($"Loại câu hỏi '{questionTypeName}' không được hỗ trợ import qua Excel.");
        }
    }
}
