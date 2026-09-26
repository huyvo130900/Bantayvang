namespace BanTayVang.API.DTOs.Exam
{
    public class ExamQuestionDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public int QuestionOrder { get; set; }
        public List<ExamChoiceDto> Options { get; set; } = new();
        
        // Câu trả lời của thí sinh (nếu đã trả lời)
        public int? SelectedOptionId { get; set; }
        public List<int> SelectedOptionIdList { get; set; } = new();
        public string? EssayAnswer { get; set; }
        // BUG FIX: SubmitAnswerDto/SubmissionDetail already carry an essay-answer image (see
        // question-display.tsx's essay image upload), but this DTO never surfaced it back when
        // restoring a session (page reload / resume) - the frontend read `essayImageUrl` off this
        // exact object anyway (via an `as any` cast, since the field didn't exist), so it always
        // came back undefined and a previously-uploaded essay image silently vanished from the UI
        // on reload even though it was still saved server-side.
        public string? EssayImageUrl { get; set; }
        public bool IsSaved { get; set; }
        public bool AllowMultipleSelection { get; set; }
    }
}