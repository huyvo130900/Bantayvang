namespace BanTayVang.API.Helpers
{
    /// <summary>
    /// Quy tắc "Đạt/Không đạt" dùng chung cho mọi nơi tính Pass trong ứng dụng (trước đây copy-paste
    /// độc lập ở GradingService.cs x3, ExamService.cs, ExamAssignmentService.cs - dễ lệch nhau khi
    /// sửa 1 chỗ mà quên chỗ khác, đúng kiểu lỗi "Đạt ở trang này, Không đạt ở trang kia" đã gặp).
    /// Ưu tiên ngưỡng của ExamCampaign; nếu kỳ thi không cấu hình mới dùng ngưỡng của ExamPaper;
    /// nếu cả 2 đều không cấu hình thì coi là Đạt (không có ngưỡng nào để so sánh).
    /// </summary>
    public static class PassRuleHelper
    {
        public static bool ComputePass(int? correctAnswers, int? campaignMinPassQuestions, int? paperMinPassQuestions)
        {
            var minPassQuestions = campaignMinPassQuestions ?? paperMinPassQuestions;
            return minPassQuestions == null || (correctAnswers ?? 0) >= minPassQuestions.Value;
        }
    }
}
