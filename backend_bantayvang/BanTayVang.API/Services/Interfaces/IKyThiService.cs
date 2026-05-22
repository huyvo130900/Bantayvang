using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.KyThi;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IKyThiService
    {
        Task<BaseResponseDto<List<KyThiDto>>> GetAllAsync(string? trangThai = null);
        Task<BaseResponseDto<KyThiDto>> GetByIdAsync(int id);
        Task<BaseResponseDto<KyThiDto>> CreateAsync(CreateKyThiDto dto, int nguoiTao);
        Task<BaseResponseDto<KyThiDto>> UpdateAsync(int id, UpdateKyThiDto dto);
        Task<BaseResponseDto> UpdateStatusAsync(int id, string trangThai);
        Task<BaseResponseDto> DeleteAsync(int id);

        // Ca thi
        Task<BaseResponseDto<List<CaThiDto>>> GetCaThiByKyThiAsync(int kyThiId);
        Task<BaseResponseDto<CaThiDto>> CreateCaThiAsync(CreateCaThiDto dto);
        Task<BaseResponseDto<CaThiDto>> UpdateCaThiAsync(int id, CreateCaThiDto dto);
        Task<BaseResponseDto> DeleteCaThiAsync(int id);
    }
}