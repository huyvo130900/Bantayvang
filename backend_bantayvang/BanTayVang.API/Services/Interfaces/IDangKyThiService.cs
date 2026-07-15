using System.Collections.Generic;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.DangKyThi;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IDangKyThiService
    {
        Task<DangKyThiDto> CreateAsync(CreateDangKyThiDto dto);
        Task<IEnumerable<DangKyThiDto>> GetPendingAsync(int? khoaPhongId = null);
        Task<bool> ApproveAsync(int id, int userId);
        Task<bool> RejectAsync(int id, string? reason, int userId);
    }
}
