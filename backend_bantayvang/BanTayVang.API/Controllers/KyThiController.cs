using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.KyThi;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var result = await _kyThiService.GetAllAsync(trangThai);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> GetById(int id)
        {
            var result = await _kyThiService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> Create([FromBody] CreateKyThiDto dto)
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _kyThiService.CreateAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<KyThiDto>>> Update(int id, [FromBody] UpdateKyThiDto dto)
        {
            var result = await _kyThiService.UpdateAsync(id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/status")]
        public async Task<ActionResult<BaseResponseDto>> UpdateStatus(int id, [FromBody] string trangThai)
        {
            var result = await _kyThiService.UpdateStatusAsync(id, trangThai);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseDto>> Delete(int id)
        {
            var result = await _kyThiService.DeleteAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ===== Ca Thi =====

        [HttpGet("{kyThiId}/ca-thi")]
        public async Task<ActionResult<BaseResponseDto<List<CaThiDto>>>> GetCaThi(int kyThiId)
        {
            var result = await _kyThiService.GetCaThiByKyThiAsync(kyThiId);
            return Ok(result);
        }

        [HttpPost("ca-thi")]
        public async Task<ActionResult<BaseResponseDto<CaThiDto>>> CreateCaThi([FromBody] CreateCaThiDto dto)
        {
            var result = await _kyThiService.CreateCaThiAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("ca-thi/{id}")]
        public async Task<ActionResult<BaseResponseDto<CaThiDto>>> UpdateCaThi(int id, [FromBody] CreateCaThiDto dto)
        {
            var result = await _kyThiService.UpdateCaThiAsync(id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("ca-thi/{id}")]
        public async Task<ActionResult<BaseResponseDto>> DeleteCaThi(int id)
        {
            var result = await _kyThiService.DeleteCaThiAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}