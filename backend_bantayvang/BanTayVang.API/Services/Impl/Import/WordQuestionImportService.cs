using System.Text;
using System.Text.RegularExpressions;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Import;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl.Import
{
    public class WordQuestionImportService : IWordQuestionImportService
    {
        private readonly ILogger<WordQuestionImportService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly BanTayVangDbContext _context;
        private readonly IQuestionImportStrategyFactory _strategyFactory;
        private readonly IQuestionRepository _questionRepository;
        private MainDocumentPart? _currentMainDocumentPart;

        private static readonly Regex QuestionStartRegex =
            new(@"^[Cc]â[uU]\s+(\d+)\s*[:\.]\s*(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex OptionRegex =
            new(@"^([A-Da-d])\s*[\.\)]\s*(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex DifficultyRegex =
            new(@"^[Mm]ức\s+độ\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex ExplanationRegex =
            new(@"^(?:[Gg]iải\s+thích|[Ll]ời\s+giải|[Hh]ướng\s+dẫn)\s*:\s*(.*)",
                RegexOptions.Compiled | RegexOptions.Singleline);
        // Đáp án mẫu / gợi ý cho câu Tự luận (không bắt buộc)
        private static readonly Regex SuggestedAnswerRegex =
            new(@"^(?:[Đđ]áp\s+án\s+mẫu|Gợi\s+ý)\s*:\s*(.*)",
                RegexOptions.Compiled | RegexOptions.Singleline);

        private enum ParseState { None, InQuestion, InOptions }

        public WordQuestionImportService(
            ILogger<WordQuestionImportService> logger,
            IFileUploadService fileUploadService,
            BanTayVangDbContext context,
            IQuestionImportStrategyFactory strategyFactory,
            IQuestionRepository questionRepository)
        {
            _logger = logger;
            _fileUploadService = fileUploadService;
            _context = context;
            _strategyFactory = strategyFactory;
            _questionRepository = questionRepository;
        }

        public async Task<List<Question>> ParseAndValidateAsync(
            IFormFile file,
            int createdBy,
            string department,
            int questionCategoryId,
            List<string> errors)
        {
            var result = new List<Question>();
            try
            {
                var category = await _context.QuestionCategories.FindAsync(questionCategoryId);
                if (category == null)
                {
                    errors.Add("Không tìm thấy loại câu hỏi.");
                    return result;
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var wordDoc = WordprocessingDocument.Open(stream, false);
                _currentMainDocumentPart = wordDoc.MainDocumentPart;
                if (_currentMainDocumentPart == null) return result;

                var paragraphItems = ExtractParagraphItems(wordDoc);
                result = await BuildQuestionsAsync(paragraphItems, createdBy, department, category, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc file Word.");
                errors.Add($"Không thể đọc file Word: {ex.Message}");
            }
            return result;
        }

        private record ParagraphItem(string FullText, bool FirstCharIsBoldOrUnderline, string? ImageEmbedId);

        private static List<ParagraphItem> ExtractParagraphItems(WordprocessingDocument wordDoc)
        {
            var items = new List<ParagraphItem>();
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return items;

            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var drawing = para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().FirstOrDefault();
                string? imageEmbedId = null;
                if (drawing != null)
                {
                    var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                    imageEmbedId = blip?.Embed?.Value;
                }

                var fullText = para.InnerText.Trim();
                if (string.IsNullOrEmpty(fullText))
                {
                    items.Add(new ParagraphItem(string.Empty, false, imageEmbedId));
                    continue;
                }

                bool isMarked = IsFirstMeaningfulRunMarked(para);
                items.Add(new ParagraphItem(fullText, isMarked, imageEmbedId));
            }
            return items;
        }

        private static bool IsFirstMeaningfulRunMarked(DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
        {
            foreach (var run in para.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
            {
                var text = run.InnerText;
                if (string.IsNullOrWhiteSpace(text)) continue;

                // BUG FIX: a run with no explicit RunProperties (very common for plain,
                // unformatted text - Word does not always emit a <w:rPr> element) used to make
                // this method `continue` to the NEXT run instead of concluding "not bold/underlined".
                // That let an unrelated later run in the same paragraph (e.g. trailing punctuation
                // that happens to carry bold formatting) decide the result, so a perfectly plain
                // answer could be misdetected as "correct" after common Word editing operations
                // split a paragraph into several runs. A missing RunProperties simply means no
                // formatting was applied, i.e. not bold and not underlined.
                var props = run.RunProperties;
                bool isBold = props?.Bold != null &&
                              props.Bold.Val?.ToString()?.ToLower() != "false" &&
                              props.Bold.Val?.ToString()?.ToLower() != "0";

                bool isUnderline = props?.Underline != null &&
                                   props.Underline.Val != null &&
                                   props.Underline.Val != UnderlineValues.None;

                return isBold || isUnderline;
            }
            return false;
        }

        private async Task<List<Question>> BuildQuestionsAsync(
            List<ParagraphItem> items,
            int createdBy,
            string department,
            QuestionCategory category,
            List<string> errors)
        {
            var strategy = _strategyFactory.GetStrategy(category.CategoryName ?? "");
            bool isEssayMode = strategy is EssayImportStrategy;

            var questions = new List<Question>();
            Question? currentQuestion = null;
            string? pendingImageEmbedId = null;
            var currentContent = new StringBuilder();
            var currentOptions = new List<(char Letter, string Content, bool IsCorrect)>();
            string? currentDifficulty = null;
            string? currentExplanation = null;
            string? currentSuggestedAnswer = null;
            int questionNumber = 0;
            var state = ParseState.None;
            // BUG FIX: unlike the Excel import strategies, this Word import path never checked for
            // duplicate content (neither within the same file nor against the existing question bank),
            // so importing a Word file could silently create duplicate questions. Mirror the same
            // in-file + DB duplicate check the Excel strategies already use.
            var seenContents = new HashSet<string>();

            async Task FinalizeCurrentQuestionAsync()
            {
                if (currentQuestion == null) return;

                var qContent = currentContent.ToString().Trim();
                if (string.IsNullOrEmpty(qContent))
                {
                    errors.Add($"Câu {questionNumber}: Nội dung câu hỏi bị trống.");
                    return;
                }

                var standardizedContent = qContent.Trim().ToLower();
                if (!seenContents.Add(standardizedContent))
                {
                    errors.Add($"Câu {questionNumber}: Câu hỏi trùng lặp trong cùng file Word — \"{qContent}\" — bỏ qua.");
                    return;
                }
                if (department != "Không thuộc ngân hàng")
                {
                    var existingQuestion = await _questionRepository.FindDuplicateAsync(standardizedContent, department);
                    if (existingQuestion != null)
                    {
                        errors.Add($"Câu {questionNumber}: Câu hỏi đã tồn tại trong CSDL (Id: {existingQuestion.Id}) — \"{qContent}\"");
                        return;
                    }
                }

                if (isEssayMode && currentOptions.Count > 0)
                {
                    errors.Add($"Câu {questionNumber}: Đây là câu Tự luận, không được có đáp án A/B/C/D.");
                    return;
                }
                if (!isEssayMode && currentOptions.Count < 2)
                {
                    errors.Add($"Câu {questionNumber}: Câu trắc nghiệm phải có ít nhất 2 đáp án.");
                    return;
                }
                if (!isEssayMode && !currentOptions.Any(o => o.IsCorrect))
                {
                    errors.Add($"Câu {questionNumber}: Chưa có đáp án đúng. Vui lòng bôi đậm/gạch chân chữ cái đáp án đúng.");
                    return;
                }

                currentQuestion.Content = qContent;
                currentQuestion.Difficulty = currentDifficulty ?? "2";
                currentQuestion.Department = department;
                currentQuestion.QuestionCategoryId = category.Id;
                currentQuestion.CreatedBy = createdBy;
                currentQuestion.CreatedAt = DateTime.UtcNow.AddHours(7);
                currentQuestion.IsDeleted = false;
                // Đáp án mẫu (chỉ áp dụng cho Tự luận, bỏ trống cũng không sao)
                if (!string.IsNullOrWhiteSpace(currentSuggestedAnswer))
                    currentQuestion.SuggestedAnswer = currentSuggestedAnswer;

                if (pendingImageEmbedId != null)
                {
                    var imageUrl = await TryUploadQuestionImageAsync(pendingImageEmbedId, department, category.CategoryName ?? "", questionNumber, errors);
                    if (imageUrl != null)
                    {
                        currentQuestion.ImageUrl = imageUrl;
                    }
                }

                int idx = 1;
                foreach (var (letter, content, isCorrect) in currentOptions)
                {
                    currentQuestion.QuestionOptions.Add(new QuestionOption
                    {
                        Content = content,
                        IsCorrect = isCorrect,
                        OrderIndex = idx++
                    });
                }

                questions.Add(currentQuestion);
            }

            foreach (var item in items)
            {
                var text = item.FullText;
                var qMatch = QuestionStartRegex.Match(text);
                if (qMatch.Success)
                {
                    // BUG FIX: this block used to run AFTER a generic "attach pending image to
                    // currentQuestion" check that ran unconditionally at the top of the loop.
                    // When a paragraph both carried an image AND started a new "Câu N:" question,
                    // that generic check fired first - using the STILL-OLD currentQuestion - so the
                    // image ended up staged for the PREVIOUS question (finalized right below) instead
                    // of the new one starting on this very line. Handling the image assignment here,
                    // after currentQuestion has already been replaced, keeps it with the right question.
                    await FinalizeCurrentQuestionAsync();
                    questionNumber = int.Parse(qMatch.Groups[1].Value);
                    currentQuestion = new Question();
                    currentContent.Clear();
                    currentContent.Append(qMatch.Groups[2].Value.Trim());
                    currentOptions = new List<(char, string, bool)>();
                    currentDifficulty = null;
                    currentExplanation = null;
                    currentSuggestedAnswer = null;
                    pendingImageEmbedId = item.ImageEmbedId; // If image is on the same line
                    state = ParseState.InQuestion;
                    continue;
                }

                if (item.ImageEmbedId != null && currentQuestion != null && pendingImageEmbedId == null)
                {
                    pendingImageEmbedId = item.ImageEmbedId;
                }

                var optMatch = OptionRegex.Match(text);
                if (optMatch.Success && state != ParseState.None)
                {
                    char letter = char.ToUpper(optMatch.Groups[1].Value[0]);
                    string content = optMatch.Groups[2].Value.Trim();
                    // BUG FIX: MultipleChoiceImportStrategy (Excel) only adds a choice when its
                    // content is non-blank; this Word parser added every "A./B./C./D." line
                    // regardless, so a line like "B." with nothing typed after it (a common typo
                    // when authoring in Word) became a real, saved QuestionOption with empty
                    // Content - an empty answer slot shown to the student while taking the exam.
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        state = ParseState.InOptions;
                        continue;
                    }
                    bool isCorrect = item.FirstCharIsBoldOrUnderline;
                    currentOptions.Add((letter, content, isCorrect));
                    state = ParseState.InOptions;
                    continue;
                }

                var diffMatch = DifficultyRegex.Match(text);
                if (diffMatch.Success && currentQuestion != null)
                {
                    currentDifficulty = diffMatch.Groups[1].Value.Trim();
                    continue;
                }

                var expMatch = ExplanationRegex.Match(text);
                if (expMatch.Success && currentQuestion != null)
                {
                    currentExplanation = expMatch.Groups[1].Value.Trim();
                    continue;
                }

                var sugMatch = SuggestedAnswerRegex.Match(text);
                if (sugMatch.Success && currentQuestion != null)
                {
                    currentSuggestedAnswer = sugMatch.Groups[1].Value.Trim();
                    continue;
                }

                if (string.IsNullOrEmpty(text)) continue;

                if (state == ParseState.InQuestion && currentQuestion != null)
                {
                    currentContent.Append(" " + text);
                }
            }

            await FinalizeCurrentQuestionAsync();
            return questions;
        }

        private async Task<string?> TryUploadQuestionImageAsync(string embedId, string department, string categoryName, int questionNumber, List<string> errors)
        {
            if (_currentMainDocumentPart == null) return null;
            var part = _currentMainDocumentPart.GetPartById(embedId);
            if (part is not ImagePart imagePart) return null;

            var contentType = imagePart.ContentType;
            if (contentType.Contains("emf") || contentType.Contains("wmf"))
            {
                errors.Add($"Câu {questionNumber}: Ảnh ở định dạng vector (EMF/WMF) không được hỗ trợ. Vui lòng chèn ảnh dạng JPG/PNG trực tiếp thay vì dán (paste) từ ứng dụng khác.");
                return null;
            }

            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                _ => null
            };
            if (extension == null)
            {
                errors.Add($"Câu {questionNumber}: Định dạng ảnh '{contentType}' không được hỗ trợ.");
                return null;
            }

            using var imageStream = imagePart.GetStream();
            using var buffer = new MemoryStream();
            await imageStream.CopyToAsync(buffer);
            buffer.Position = 0;

            var fakeFile = new FormFile(buffer, 0, buffer.Length, "image", $"word-image-{embedId}{extension}")
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            var safeDept = Slugify(department);
            var safeCategory = Slugify(categoryName);
            var folderPath = $"questions/{safeDept}/{safeCategory}/{DateTime.UtcNow.AddHours(7):yyyy-MM}";

            var uploadResult = await _fileUploadService.UploadImageAsync(fakeFile, folderPath);
            if (!uploadResult.Success)
            {
                errors.Add($"Câu {questionNumber}: Upload ảnh thất bại - {uploadResult.Message}");
                return null;
            }
            return uploadResult.FileUrl;
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "khac";
            var normalized = input.Trim().ToLowerInvariant();
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(normalized) ? "khac" : normalized;
        }

        public byte[] GenerateTemplateDocx()
        {
            var templatePath = System.IO.Path.Combine("wwwroot", "templates", "Mau_Import_CauHoi_Azota.docx");
            if (File.Exists(templatePath))
                return File.ReadAllBytes(templatePath);
            return Array.Empty<byte>();
        }
    }
}

