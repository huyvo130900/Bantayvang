using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.ExamCampaign;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BanTayVang.API.Attributes;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class ExamCampaignController : ControllerBase
    {
        private readonly IExamCampaignService _examCampaignService;

        public ExamCampaignController(IExamCampaignService kyThiService)
        {
            _examCampaignService = kyThiService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<ExamCampaignDto>>>> GetAll([FromQuery] string? status = null)
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _examCampaignService.GetAllAsync(status, userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> GetById(int id)
        {
            var result = await _examCampaignService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null || result.Data.DepartmentId != myKhoaId)
                    return Forbid();
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> Create([FromBody] CreateKyThiDto dto)
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto<ExamCampaignDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                dto.DepartmentId = myKhoaId;
            }

            var result = await _examCampaignService.CreateAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> Update(int id, [FromBody] UpdateKyThiDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto<ExamCampaignDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.DepartmentId != myKhoaId)
                {
                    return Forbid();
                }
                dto.DepartmentId = myKhoaId;
            }

            var result = await _examCampaignService.UpdateAsync(id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/status")]
        public async Task<ActionResult<BaseResponseDto>> UpdateStatus(int id, [FromBody] string status)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                if (myKhoaId == null)
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.DepartmentId != myKhoaId)
                {
                    return Forbid();
                }
            }

            var result = await _examCampaignService.UpdateStatusAsync(id, status);
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
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || existing.Data.DepartmentId != myKhoaId)
                {
                    return Forbid();
                }
            }

            var result = await _examCampaignService.DeleteAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ===== Sinh đề cho kỳ thi =====

        [HttpPost("{examCampaignId}/check-generation")]
        public async Task<ActionResult<BaseResponseDto<ExamCheckResultDto>>> CheckGeneration(int examCampaignId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                var myKhoaName = DepartmentAuthHelper.GetKhoaPhong(User);
                if (myKhoaId == null || string.IsNullOrEmpty(myKhoaName))
                {
                    return BadRequest(BaseResponseDto<ExamCheckResultDto>.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (!existing.Success || existing.Data == null || existing.Data.DepartmentId != myKhoaId)
                {
                    return Forbid();
                }
                config.Department = myKhoaName;
            }

            var result = await _examCampaignService.CheckExamsAvailabilityAsync(examCampaignId, config);
            return Ok(result);
        }

        [HttpPost("{examCampaignId}/generate-exams")]
        public async Task<ActionResult<BaseResponseDto>> GenerateExams(int examCampaignId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                var myKhoaName = DepartmentAuthHelper.GetKhoaPhong(User);
                if (myKhoaId == null || string.IsNullOrEmpty(myKhoaName))
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (!existing.Success || existing.Data == null || existing.Data.DepartmentId != myKhoaId)
                {
                    return Forbid();
                }
                config.Department = myKhoaName;
            }

            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _examCampaignService.GenerateExamsForCampaignAsync(examCampaignId, config, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}