using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.KyThi;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BanTayVang.API.Attributes;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class KyThiController : ControllerBase
    {
        private readonly IKyThiService _kyThiService;

        public KyThiController(IKyThiService kyThiService)
        {
            _kyThiService = kyThiService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<KyThiDto>>>> GetAll([FromQuery] string? trangThai = null)
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _kyThiService.GetAllAsync(trangThai, userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> GetById(int id)
        {
            var result = await _kyThiService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null || result.Data.KhoaPhongId != myKhoaId)
                    return Forbid();
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> Create([FromBody] CreateKyThiDto dto)
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto<KyThiDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                dto.KhoaPhongId = myKhoaId;
            }

            var result = await _kyThiService.CreateAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> Update(int id, [FromBody] UpdateKyThiDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto<KyThiDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _kyThiService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.KhoaPhongId != myKhoaId)
                {
                    return Forbid();
                }
                dto.KhoaPhongId = myKhoaId;
            }

            var result = await _kyThiService.UpdateAsync(id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/status")]
        public async Task<ActionResult<BaseResponseDto>> UpdateStatus(int id, [FromBody] string trangThai)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _kyThiService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.KhoaPhongId != myKhoaId)
                {
                    return Forbid();
                }
            }

            var result = await _kyThiService.UpdateStatusAsync(id, trangThai);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseDto>> Delete(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _kyThiService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.KhoaPhongId != myKhoaId)
                {
                    return Forbid();
                }
            }

            var result = await _kyThiService.DeleteAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ===== Sinh đề cho kỳ thi =====

        [HttpPost("{kyThiId}/check-generation")]
        public async Task<ActionResult<BaseResponseDto<ExamCheckResultDto>>> CheckGeneration(int kyThiId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                var myKhoaName = DepartmentAuthHelper.GetKhoaPhong(User);
                if (myKhoaId == null || string.IsNullOrEmpty(myKhoaName))
                {
                    return BadRequest(BaseResponseDto<ExamCheckResultDto>.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _kyThiService.GetByIdAsync(kyThiId);
                if (!existing.Success || existing.Data == null || existing.Data.KhoaPhongId != myKhoaId)
                {
                    return Forbid();
                }
                config.KhoaPhong = myKhoaName;
            }

            var result = await _kyThiService.CheckExamsAvailabilityAsync(kyThiId, config);
            return Ok(result);
        }

        [HttpPost("{kyThiId}/generate-exams")]
        public async Task<ActionResult<BaseResponseDto>> GenerateExams(int kyThiId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                var myKhoaName = DepartmentAuthHelper.GetKhoaPhong(User);
                if (myKhoaId == null || string.IsNullOrEmpty(myKhoaName))
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _kyThiService.GetByIdAsync(kyThiId);
                if (!existing.Success || existing.Data == null || existing.Data.KhoaPhongId != myKhoaId)
                {
                    return Forbid();
                }
                config.KhoaPhong = myKhoaName;
            }

            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _kyThiService.GenerateExamsForKyThiAsync(kyThiId, config, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}