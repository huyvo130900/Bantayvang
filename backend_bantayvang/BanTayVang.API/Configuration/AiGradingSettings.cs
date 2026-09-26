namespace BanTayVang.API.Configuration
{
    public class AiGradingSettings
    {
        public const string SectionName = "AiGrading";

        /// <summary>Provider: Gemini hoac OpenAI</summary>
        public string Provider { get; set; } = "Gemini";

        /// <summary>API Key cua provider</summary>
        public string ApiKey { get; set; } = "";

        /// <summary>Ten model (vi du: gemini-2.0-flash)</summary>
        public string Model { get; set; } = "gemini-2.0-flash";

        /// <summary>So lan retry toi da khi API loi</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Timeout moi lan goi AI (giay)</summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
