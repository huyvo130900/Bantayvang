using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.ExamCampaign;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BanTayVang.API.Attributes;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class ExamCampaignController : ControllerBase
    {
        private readonly IExamCampaignService _examCampaignService;

        public ExamCampaignController(IExamCampaignService examCampaignService)
        {
            _examCampaignService = examCampaignService;
        }

        // BUG FIX (critical, confirmed live): this was ManagementOnly, but exam-waiting-page.tsx -
        // the ONLY entry point student/ThiSinhNgoai roles land on after login (see
        // ProtectedRoute's getDefaultRedirect and the /exam-waiting route) - calls exactly this
        // endpoint to list campaigns to take. A real student account got a flat 403 here, meaning
        // they could never see any exam to start. ExamCampaignService.GetAllAsync already has
        // department-scoped filtering built in for RoleId==3 specifically for this purpose - it was
        // simply unreachable because the controller blocked non-management callers before that
        // code could ever run. Falls back to the class-level [RequireAuth] (any authenticated
        // user); the service method does the actual per-role scoping.
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<ExamCampaignDto>>>> GetAll([FromQuery] string? status = null)
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _examCampaignService.GetAllAsync(status, userId.Value);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> GetById(int id)
        {
            var result = await _examCampaignService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                if (myDepartmentId == null || !result.Data.DepartmentIds.Contains(myDepartmentId.Value))
                    return Forbid();
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> Create([FromBody] CreateExamCampaignDto dto)
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                if (myDepartmentId == null)
                {
                    return BadRequest(BaseResponseDto<ExamCampaignDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                dto.DepartmentIds = new List<int> { myDepartmentId.Value };
            }

            var result = await _examCampaignService.CreateAsync(dto, userId.Value);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamCampaignDto>>> Update(int id, [FromBody] UpdateExamCampaignDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                if (myDepartmentId == null)
                {
                    return BadRequest(BaseResponseDto<ExamCampaignDto>.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
                dto.DepartmentIds = new List<int> { myDepartmentId.Value };
            }

            var result = await _examCampaignService.UpdateAsync(id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/status")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> UpdateStatus(int id, [FromBody] string status)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                if (myDepartmentId == null)
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
            }

            var result = await _examCampaignService.UpdateStatusAsync(id, status);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> Delete(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                if (myDepartmentId == null)
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(id);
                if (!existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
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
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamCheckResultDto>>> CheckGeneration(int examCampaignId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                var myDepartmentName = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (myDepartmentId == null || string.IsNullOrEmpty(myDepartmentName))
                {
                    return BadRequest(BaseResponseDto<ExamCheckResultDto>.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (!existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
                config.Department = myDepartmentName;
            }

            var result = await _examCampaignService.CheckExamsAvailabilityAsync(examCampaignId, config);
            return Ok(result);
        }

        [HttpPost("{examCampaignId}/generate-exams")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> GenerateExams(int examCampaignId, [FromBody] ExamGenerationConfigDto config)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                var myDepartmentName = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (myDepartmentId == null || string.IsNullOrEmpty(myDepartmentName))
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa được cấu hình khoa phòng quản lý."));
                }
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (!existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
                config.Department = myDepartmentName;
            }

            var userId = HttpContext.Items["UserId"] as int?;
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _examCampaignService.GenerateExamsForCampaignAsync(examCampaignId, config, userId.Value);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Báo cáo "ai đủ điều kiện thi / ai đã thi" cho 1 kỳ thi - đáp ứng góp ý "lọc danh sách
        /// thi, có những người nào được thi".
        /// </summary>
        [HttpGet("{examCampaignId}/eligibility")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamCampaignEligibilityDto>>>> GetEligibility(int examCampaignId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (myDepartmentId == null || !existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
            }

            var result = await _examCampaignService.GetEligibilityAsync(examCampaignId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Úp danh sách (Excel/CSV) mã nhân viên/tài khoản để chỉ định chính xác ai được thi - áp
        /// dụng cho kỳ thi ở chế độ "Danh sách chỉ định" (AccessMode = AssignedList).
        /// </summary>
        [HttpPost("{examCampaignId}/assign-from-excel")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<AssignFromExcelResultDto>>> AssignFromExcel(int examCampaignId, IFormFile file)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                var existing = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (myDepartmentId == null || !existing.Success || existing.Data == null || !existing.Data.DepartmentIds.Contains(myDepartmentId.Value))
                {
                    return Forbid();
                }
            }

            var result = await _examCampaignService.AssignFromExcelAsync(examCampaignId, file);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
