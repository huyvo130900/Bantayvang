using BanTayVang.API.DTOs.Category;
using BanTayVang.API.DTOs.Common;
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
        private readonly ILoaicauhoiRepository _loaiRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            ILoaicauhoiRepository loaiRepository,
            ILogger<CategoryService> logger)
        {
            _loaiRepository = loaiRepository;
            _logger = logger;
        }

        #region Question Type (Loai cau hoi) Operations

        public async Task<BaseResponseDto<List<LoaicauhoiDto>>> GetAllQuestionTypesAsync()
        {
            try
            {
                var types = await _loaiRepository.GetAllAsync();
                var result = new List<LoaicauhoiDto>();
                
                foreach (var t in types)
                {
                    result.Add(new LoaicauhoiDto
                    {
                        Id = t.Id,
                        TenLoai = t.TenLoai,
                        MoTa = t.MoTa,
                        SoCauHoi = await _loaiRepository.GetQuestionCountAsync(t.Id)
                    });
                }

                return new BaseResponseDto<List<LoaicauhoiDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách loại câu hỏi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all question types");
                return new BaseResponseDto<List<LoaicauhoiDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<LoaicauhoiDto>> GetQuestionTypeByIdAsync(int id)
        {
            try
            {
                var type = await _loaiRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto<LoaicauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại câu hỏi"
                    };
                }

                return new BaseResponseDto<LoaicauhoiDto>
                {
                    Success = true,
                    Message = "Lấy thông tin loại câu hỏi thành công",
                    Data = new LoaicauhoiDto
                    {
                        Id = type.Id,
                        TenLoai = type.TenLoai,
                        MoTa = type.MoTa,
                        SoCauHoi = await _loaiRepository.GetQuestionCountAsync(type.Id)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question type by id");
                return new BaseResponseDto<LoaicauhoiDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<LoaicauhoiDto>> CreateQuestionTypeAsync(CreateLoaicauhoiDto createDto)
        {
            try
            {
                if (await _loaiRepository.ExistsByNameAsync(createDto.TenLoai))
                {
                    return new BaseResponseDto<LoaicauhoiDto>
                    {
                        Success = false,
                        Message = "Tên loại câu hỏi đã tồn tại"
                    };
                }

                var type = new Loaicauhoi
                {
                    TenLoai = createDto.TenLoai,
                    MoTa = createDto.MoTa
                };

                var saved = await _loaiRepository.AddAsync(type);

                return new BaseResponseDto<LoaicauhoiDto>
                {
                    Success = true,
                    Message = "Tạo loại câu hỏi thành công",
                    Data = new LoaicauhoiDto
                    {
                        Id = saved.Id,
                        TenLoai = saved.TenLoai,
                        MoTa = saved.MoTa,
                        SoCauHoi = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question type");
                return new BaseResponseDto<LoaicauhoiDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo loại câu hỏi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<LoaicauhoiDto>> UpdateQuestionTypeAsync(int id, CreateLoaicauhoiDto updateDto)
        {
            try
            {
                var type = await _loaiRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto<LoaicauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại câu hỏi"
                    };
                }

                if (await _loaiRepository.ExistsByNameAsync(updateDto.TenLoai, id))
                {
                    return new BaseResponseDto<LoaicauhoiDto>
                    {
                        Success = false,
                        Message = "Tên loại câu hỏi đã tồn tại"
                    };
                }

                type.TenLoai = updateDto.TenLoai;
                type.MoTa = updateDto.MoTa;
                await _loaiRepository.UpdateAsync(type);

                return new BaseResponseDto<LoaicauhoiDto>
                {
                    Success = true,
                    Message = "Cập nhật loại câu hỏi thành công",
                    Data = new LoaicauhoiDto
                    {
                        Id = type.Id,
                        TenLoai = type.TenLoai,
                        MoTa = type.MoTa,
                        SoCauHoi = await _loaiRepository.GetQuestionCountAsync(type.Id)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question type");
                return new BaseResponseDto<LoaicauhoiDto>
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
                var type = await _loaiRepository.GetByIdAsync(id);
                if (type == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy loại câu hỏi" };
                }

                var questionCount = await _loaiRepository.GetQuestionCountAsync(id);
                if (questionCount > 0)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = $"Không thể xóa loại câu hỏi đang có {questionCount} câu hỏi"
                    };
                }

                await _loaiRepository.DeleteAsync(id);

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