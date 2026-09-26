using BanTayVang.API.DTOs.Category;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Category service implementation following SOLID principles
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly IQuestionCategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            IQuestionCategoryRepository loaiRepository,
            ILogger<CategoryService> logger)
        {
            _categoryRepository = loaiRepository;
            _logger = logger;
        }

        #region Question Type (Loai cau hoi) Operations

        public async Task<BaseResponseDto<List<QuestionCategoryDto>>> GetAllQuestionTypesAsync()
        {
            try
            {
                var types = await _categoryRepository.GetAllAsync();
                var result = new List<QuestionCategoryDto>();
                
                foreach (var t in types)
                {
                    result.Add(new QuestionCategoryDto
                    {
                        Id = t.Id,
                        CategoryName = t.CategoryName,
                        Description = t.Description,
                        TotalQuestions = await _categoryRepository.GetQuestionCountAsync(t.Id)
                    });
                }

                return new BaseResponseDto<List<QuestionCategoryDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách loại câu hỏi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all question types");
                return new BaseResponseDto<List<QuestionCategoryDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionCategoryDto>> GetQuestionTypeByIdAsync(int id)
        {
            try
            {
                var type = await _categoryRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto<QuestionCategoryDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại câu hỏi"
                    };
                }

                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = true,
                    Message = "Lấy thông tin loại câu hỏi thành công",
                    Data = new QuestionCategoryDto
                    {
                        Id = type.Id,
                        CategoryName = type.CategoryName,
                        Description = type.Description,
                        TotalQuestions = await _categoryRepository.GetQuestionCountAsync(type.Id)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question type by id");
                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionCategoryDto>> CreateQuestionTypeAsync(CreateQuestionCategoryDto createDto)
        {
            try
            {
                if (await _categoryRepository.ExistsByNameAsync(createDto.CategoryName))
                {
                    return new BaseResponseDto<QuestionCategoryDto>
                    {
                        Success = false,
                        Message = "Tên loại câu hỏi đã tồn tại"
                    };
                }

                var type = new QuestionCategory
                {
                    CategoryName = createDto.CategoryName,
                    Description = createDto.Description
                };

                var saved = await _categoryRepository.AddAsync(type);

                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = true,
                    Message = "Tạo loại câu hỏi thành công",
                    Data = new QuestionCategoryDto
                    {
                        Id = saved.Id,
                        CategoryName = saved.CategoryName,
                        Description = saved.Description,
                        TotalQuestions = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question type");
                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionCategoryDto>> UpdateQuestionTypeAsync(int id, CreateQuestionCategoryDto updateDto)
        {
            try
            {
                var type = await _categoryRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto<QuestionCategoryDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại câu hỏi"
                    };
                }

                if (await _categoryRepository.ExistsByNameAsync(updateDto.CategoryName, id))
                {
                    return new BaseResponseDto<QuestionCategoryDto>
                    {
                        Success = false,
                        Message = "Tên loại câu hỏi đã tồn tại"
                    };
                }

                // BUG FIX: "is this an essay category" is decided purely by matching CategoryName
                // against EssayQuestionHelper.EssayCategoryNamesArray - a check baked into the SQL
                // queries that gate auto-publish (AutoPublishExpiredExamsJob), manual publish
                // (DepartmentController/GradingController), and the "pending essay grading" queue.
                // Renaming a category that already has questions - e.g. "TL" -> "Tự luận (mở)" -
                // flips whether every one of those questions is recognised as an essay, with no
                // warning: auto-publish stops seeing them as ungraded and can publish scores that
                // never counted their tự luận part, and the grading queue stops surfacing them to
                // teachers at all. Block only the destructive direction (existing questions whose
                // essay/non-essay classification would flip); a brand-new, still-empty category can
                // still be renamed freely before any question is attached to it.
                var questionCount = await _categoryRepository.GetQuestionCountAsync(id);
                if (questionCount > 0)
                {
                    bool wasEssay = EssayQuestionHelper.IsEssayCategory(type.CategoryName);
                    bool willBeEssay = EssayQuestionHelper.IsEssayCategory(updateDto.CategoryName);
                    if (wasEssay != willBeEssay)
                    {
                        return new BaseResponseDto<QuestionCategoryDto>
                        {
                            Success = false,
                            Message = $"Không thể đổi tên: danh mục này đang có {questionCount} câu hỏi sử dụng, và việc đổi tên sẽ làm thay đổi cách hệ thống nhận diện đây là câu {(wasEssay ? "tự luận" : "trắc nghiệm")} hay không - có thể làm sai lệch việc chấm điểm/công bố điểm cho các câu hỏi đó. Vui lòng tạo danh mục mới thay vì đổi tên danh mục này."
                        };
                    }
                }

                type.CategoryName = updateDto.CategoryName;
                type.Description = updateDto.Description;
                await _categoryRepository.UpdateAsync(type);

                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = true,
                    Message = "Cập nhật loại câu hỏi thành công",
                    Data = new QuestionCategoryDto
                    {
                        Id = type.Id,
                        CategoryName = type.CategoryName,
                        Description = type.Description,
                        TotalQuestions = await _categoryRepository.GetQuestionCountAsync(type.Id)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question type");
                return new BaseResponseDto<QuestionCategoryDto>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto> DeleteQuestionTypeAsync(int id)
        {
            try
            {
                var type = await _categoryRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy loại câu hỏi" };
                }

                var questionCount = await _categoryRepository.GetQuestionCountAsync(id);
                if (questionCount > 0)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = $"Không thể xóa loại câu hỏi đang có {questionCount} câu hỏi"
                    };
                }

                await _categoryRepository.DeleteAsync(id);

                return new BaseResponseDto { Success = true, Message = "Xóa loại câu hỏi thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question type");
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi xóa loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion
    }
}
