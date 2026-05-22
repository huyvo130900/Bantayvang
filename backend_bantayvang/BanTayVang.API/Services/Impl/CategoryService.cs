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
        private readonly IDanhmucauhoiRepository _danhmucRepository;
        private readonly ILoaicauhoiRepository _loaiRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            IDanhmucauhoiRepository danhmucRepository,
            ILoaicauhoiRepository loaiRepository,
            ILogger<CategoryService> logger)
        {
            _danhmucRepository = danhmucRepository;
            _loaiRepository = loaiRepository;
            _logger = logger;
        }

        #region Category (Danh muc) Operations

        public async Task<BaseResponseDto<List<DanhmucauhoiDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _danhmucRepository.GetAllAsync();
                var result = new List<DanhmucauhoiDto>();
                
                foreach (var c in categories)
                {
                    result.Add(new DanhmucauhoiDto
                    {
                        Id = c.Id,
                        TenDanhMuc = c.TenDanhMuc,
                        Mota = c.Mota,
                        SoCauHoi = await _danhmucRepository.GetQuestionCountAsync(c.Id)
                    });
                }

                return new BaseResponseDto<List<DanhmucauhoiDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách danh mục thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return new BaseResponseDto<List<DanhmucauhoiDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách danh mục",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<DanhmucauhoiDto>> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _danhmucRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return new BaseResponseDto<DanhmucauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy danh mục"
                    };
                }

                var result = new DanhmucauhoiDto
                {
                    Id = category.Id,
                    TenDanhMuc = category.TenDanhMuc,
                    Mota = category.Mota,
                    SoCauHoi = await _danhmucRepository.GetQuestionCountAsync(category.Id)
                };

                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = true,
                    Message = "Lấy thông tin danh mục thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id");
                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin danh mục",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<DanhmucauhoiDto>> CreateCategoryAsync(CreateDanhmucauhoiDto createDto)
        {
            try
            {
                if (await _danhmucRepository.ExistsByNameAsync(createDto.TenDanhMuc))
                {
                    return new BaseResponseDto<DanhmucauhoiDto>
                    {
                        Success = false,
                        Message = "Tên danh mục đã tồn tại"
                    };
                }

                var category = new Danhmucauhoi
                {
                    TenDanhMuc = createDto.TenDanhMuc,
                    Mota = createDto.Mota
                };

                var saved = await _danhmucRepository.AddAsync(category);

                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = true,
                    Message = "Tạo danh mục thành công",
                    Data = new DanhmucauhoiDto
                    {
                        Id = saved.Id,
                        TenDanhMuc = saved.TenDanhMuc,
                        Mota = saved.Mota,
                        SoCauHoi = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo danh mục",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<DanhmucauhoiDto>> UpdateCategoryAsync(int id, CreateDanhmucauhoiDto updateDto)
        {
            try
            {
                var category = await _danhmucRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return new BaseResponseDto<DanhmucauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy danh mục"
                    };
                }

                if (await _danhmucRepository.ExistsByNameAsync(updateDto.TenDanhMuc, id))
                {
                    return new BaseResponseDto<DanhmucauhoiDto>
                    {
                        Success = false,
                        Message = "Tên danh mục đã tồn tại"
                    };
                }

                category.TenDanhMuc = updateDto.TenDanhMuc;
                category.Mota = updateDto.Mota;
                await _danhmucRepository.UpdateAsync(category);

                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = true,
                    Message = "Cập nhật danh mục thành công",
                    Data = new DanhmucauhoiDto
                    {
                        Id = category.Id,
                        TenDanhMuc = category.TenDanhMuc,
                        Mota = category.Mota,
                        SoCauHoi = await _danhmucRepository.GetQuestionCountAsync(category.Id)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return new BaseResponseDto<DanhmucauhoiDto>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật danh mục",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _danhmucRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy danh mục" };
                }

                var questionCount = await _danhmucRepository.GetQuestionCountAsync(id);
                if (questionCount > 0)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = $"Không thể xóa danh mục đang có {questionCount} câu hỏi"
                    };
                }

                await _danhmucRepository.DeleteAsync(id);

                return new BaseResponseDto { Success = true, Message = "Xóa danh mục thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi xóa danh mục",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

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