namespace BanTayVang.API.DTOs.Question
{
    /// <summary>
    /// Excel import result
    /// </summary>
    public class ImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<int> CreatedQuestionIds { get; set; } = new();
    }

    public class ImportRowError
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}