using BanTayVang.API.Configuration;
using BanTayVang.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Tich hop voi Google Gemini API de cham diem cau tu luan tu dong.
    ///
    /// QUAN TRONG ve hop dong loi (contract):
    /// - Neu thi sinh bo trong cau tra loi (khong phai loi he thong) -> tra ve (0, "...") binh thuong,
    ///   vi day la mot ket qua cham hop le.
    /// - Neu co LOI THAT SU khi goi AI (sai model, sai API key, het quota, timeout, bi safety filter
    ///   chan, parse response that bai...) -> NEM EXCEPTION thay vi tra ve (0, "loi...").
    ///   Ly do: neu tra ve (0, message) nhu truoc day, AiGradingWorker se hieu nham day la
    ///   "AI da cham xong voi diem 0" roi luu ScoreObtained = 0 lam diem CHINH THUC cua thi sinh va
    ///   danh dau AiGradingStatus = "Done" - trong khi thuc chat AI chua he cham duoc cau nay.
    ///   Nem exception giup AiGradingWorker danh dau dung AiGradingStatus = "Error" va KHONG dong
    ///   ScoreObtained, de giao vien biet can cham tay thay vi bi mat tich diem 0 sai.
    /// </summary>
    public class GeminiAIGradingService : IAIGradingService
    {
        private readonly HttpClient _httpClient;
        private readonly AiGradingSettings _settings;
        private readonly ILogger<GeminiAIGradingService> _logger;
        private readonly IWebHostEnvironment _env;

        public GeminiAIGradingService(
            HttpClient httpClient,
            IOptions<AiGradingSettings> settings,
            ILogger<GeminiAIGradingService> logger,
            IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _env = env;
        }

        public async Task<(double Score, string Comment)> GradeEssayAsync(
            string questionContent,
            string? suggestedAnswer,
            string? essayAnswer,
            string? essayImageUrl,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("AI Grading ApiKey chua duoc cau hinh trong appsettings.json (AiGrading:ApiKey)");
                throw new InvalidOperationException("AI Grading chua duoc cau hinh ApiKey trong appsettings.json (AiGrading:ApiKey)");
            }

            // Bai lam trong that su -> day la KET QUA CHAM HOP LE (khong phai loi he thong)
            if (string.IsNullOrWhiteSpace(essayAnswer) && string.IsNullOrWhiteSpace(essayImageUrl))
                return (0, "Thi sinh bo trong cau tra loi");

            var prompt = BuildPrompt(questionContent, suggestedAnswer, essayAnswer);

            Exception? lastError = null;

            for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
            {
                try
                {
                    return await CallGeminiApiAsync(prompt, essayImageUrl, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Bi huy that su tu ben ngoai (vd: app dang shutdown) - khong retry, khong nuot loi
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning(ex, "Gemini API loi lan {Attempt}/{MaxRetries}", attempt, _settings.MaxRetries);
                    if (attempt < _settings.MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                    }
                }
            }

            // Het so lan retry ma van loi -> LOI HE THONG THAT SU.
            // KHONG tra ve (0, "...") nua - nem exception de Worker danh dau AiGradingStatus = "Error"
            // va KHONG gan ScoreObtained, tranh cham nham 0 diem cho thi sinh.
            _logger.LogError(lastError, "Gemini API loi sau {MaxRetries} lan thu, danh dau AiGradingStatus=Error", _settings.MaxRetries);
            throw new InvalidOperationException(
                $"Goi Gemini API that bai sau {_settings.MaxRetries} lan thu: {lastError?.Message}", lastError);
        }

        private async Task<(double Score, string Comment)> CallGeminiApiAsync(string prompt, string? imageUrl, CancellationToken ct)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var parts = new List<object> { new { text = prompt } };

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    // Lấy đường dẫn file local từ URL (VD: http://localhost:5037/uploads/essays/abc.jpg -> /uploads/essays/abc.jpg)
                    var uri = new Uri(imageUrl);
                    var relativePath = uri.AbsolutePath.TrimStart('/');
                    var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), relativePath);

                    if (File.Exists(filePath))
                    {
                        var imageBytes = await File.ReadAllBytesAsync(filePath, ct);
                        var base64 = Convert.ToBase64String(imageBytes);

                        var ext = Path.GetExtension(filePath).ToLowerInvariant();
                        var mimeType = ext switch
                        {
                            ".png" => "image/png",
                            ".webp" => "image/webp",
                            ".heic" => "image/heic",
                            ".heif" => "image/heif",
                            _ => "image/jpeg"
                        };

                        parts.Add(new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64
                            }
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Khong tim thay file anh de gui cho AI: {FilePath}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    // Loi doc file anh khong nen chan toan bo qua trinh cham - van cham dua tren text
                    _logger.LogWarning(ex, "Loi khi doc file anh gui cho AI: {ImageUrl}", imageUrl);
                }
            }

            // JSON Schema buoc AI tra ve dung dinh dang
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    response_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            score = new { type = "number", description = "0, 0.5, hoac 1" },
                            comment = new { type = "string", description = "Nhan xet ngan gon bang tieng Viet, toi da 100 ky tu" }
                        },
                        required = new[] { "score", "comment" }
                    },
                    temperature = 0.1  // Thap de dam bao ket qua nhat quan
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            var response = await _httpClient.PostAsync(url, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("Gemini API tra ve loi {StatusCode}: {Body}", response.StatusCode, errBody);
                throw new HttpRequestException($"Gemini API error {response.StatusCode}: {Truncate(errBody, 300)}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseGeminiResponse(responseBody);
        }

        private (double Score, string Comment) ParseGeminiResponse(string responseBody)
        {
            // Khong con bat exception o day nua - de loi parse (vd bi safety filter chan noi dung,
            // response rong, sai cau truc...) nem len tren va duoc GradeEssayAsync coi la 1 lan
            // goi that bai (se retry, roi cuoi cung nem InvalidOperationException that su neu het retry).
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidatesProp) || candidatesProp.GetArrayLength() == 0)
            {
                // Truong hop thuong gap: Gemini tu choi tra loi do safety filter (vd noi dung/hinh anh y te
                // bi flag nham) -> khong co candidates. Day la loi that su, khong phai diem 0 hop le.
                throw new InvalidOperationException(
                    $"Gemini khong tra ve candidates (co the bi safety filter chan). Response: {Truncate(responseBody, 300)}");
            }

            var text = candidatesProp[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            using var inner = JsonDocument.Parse(text);

            double score = 0;
            string comment = "Khong co nhan xet";

            if (inner.RootElement.TryGetProperty("score", out var scoreProp))
                score = scoreProp.GetDouble();

            if (inner.RootElement.TryGetProperty("comment", out var commentProp))
                comment = commentProp.GetString() ?? "Khong co nhan xet";

            // Validate score: chi chap nhan 0, 0.5, hoac 1
            if (score != 0 && score != 0.5 && score != 1)
            {
                _logger.LogWarning("AI tra ve score khong hop le: {Score}, fallback ve 0", score);
                score = 0;
            }

            // Gioi han do dai comment
            if (comment.Length > 150)
                comment = comment[..150];

            return (score, comment);
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s[..max] + "...";
        }

        private string BuildPrompt(string questionContent, string? suggestedAnswer, string? essayAnswer)
        {
            var baremLine = string.IsNullOrWhiteSpace(suggestedAnswer)
                ? "(Khong co dap an mau - hay cham dua tren noi dung cau hoi)"
                : suggestedAnswer;

            return $@"Ban la Giam khao cham thi Ban tay vang - ky thi ky nang dieu duong.
Hay cham cau tra loi tu luan sau day.

CAU HOI: {questionContent}
DAP AN CHUAN: {baremLine}
CAU TRA LOI CUA THI SINH: {essayAnswer}

Cham theo thang diem:
- 1 diem: Tra loi day du, chinh xac, dung ky thuat theo dap an chuan.
- 0.5 diem: Tra loi dung mot phan, con thieu y quan trong hoac sai chi tiet nho.
- 0 diem: Sai hoan toan, khong lien quan den cau hoi, hoac bo trong.

Tra ve JSON voi 2 truong: ""score"" (chi duoc la 0, 0.5 hoac 1) va ""comment"" (nhan xet ngan gon bang tieng Viet, toi da 100 ky tu, giai thich ly do cho diem nay).";
        }
    }
}
