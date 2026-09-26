using BanTayVang.API.Models;

namespace BanTayVang.API.Helpers
{
    /// <summary>
    /// Centralized helper for essay-question detection.
    /// Using a single location prevents category-name typos from drifting
    /// across GradingService, ExamSubmissionService, AiGradingWorker, etc.
    /// </summary>
    public static class EssayQuestionHelper
    {
        // All recognised category codes that mean "essay / Tự luận"
        // Exposed as string[] so Entity Framework Core can translate it to a SQL IN clause
        public static readonly string[] EssayCategoryNamesArray = new[]
        {
            "Tự luận",
            "TuLuan",
            "TL",
            "Tu luan",   // legacy typo sometimes present in older data
            "tu_luan",
            "Essay"
        };

        private static readonly HashSet<string> EssayCategoryNames = new(EssayCategoryNamesArray, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true when the question belongs to an essay category,
        /// OR when the question has no options (treats no-option questions as essays).
        /// </summary>
        public static bool IsEssay(Question? question)
        {
            if (question == null) return false;

            var categoryName = question.QuestionCategory?.CategoryName;
            if (categoryName != null && EssayCategoryNames.Contains(categoryName))
                return true;

            // Fallback: treat questions with zero choices as essays
            return question.QuestionOptions == null || question.QuestionOptions.Count == 0;
        }

        /// <summary>
        /// Lightweight overload for contexts where only the category string is available.
        /// Does NOT apply the zero-options fallback.
        /// </summary>
        public static bool IsEssayCategory(string? categoryName)
            => categoryName != null && EssayCategoryNames.Contains(categoryName);
    }
}
