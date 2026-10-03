using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/portal")]
    public class PublicApiController : ControllerBase
    {
        private readonly IPublicService _publicService;

        public PublicApiController(IPublicService publicService)
        {
            _publicService = publicService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats() => Ok(await _publicService.GetPortalStats());

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors(
            [FromQuery] string? keyword,
            [FromQuery] int? specId,
            [FromQuery] int? facilityId,
            [FromQuery] string? province,
            [FromQuery] double? minPrice,
            [FromQuery] double? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            return Ok(await _publicService.GetDoctors(keyword, specId, facilityId, province, minPrice, maxPrice, sortBy, page, size));
        }

        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctorDetail(int id) => Ok(await _publicService.GetDoctorById(id));

        [HttpGet("doctors/{id}/reviews")]
        public async Task<IActionResult> GetDoctorReviews(int id) => Ok(await _publicService.GetReviewsByDoctorId(id));

        [HttpGet("doctors/{id}/slots")]
        public async Task<IActionResult> GetAvailableSlots(int id, [FromQuery] DateOnly date) => Ok(await _publicService.GetAvailableSlots(id, date));

        [HttpGet("facilities")]
        public async Task<IActionResult> GetAllFacilities() => Ok(await _publicService.GetAllFacilities());

        [HttpGet("specialties")]
        public async Task<IActionResult> GetAllSpecialties() => Ok(await _publicService.GetAllSpecialties());
    }
}
