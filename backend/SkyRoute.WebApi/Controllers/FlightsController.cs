using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Services;

namespace SkyRoute.WebApi.Controllers;

[ApiController]
[Route("api/flights")]
public sealed class FlightsController(FlightSearchService flightSearchService) : ControllerBase
{
    [HttpPost("search")]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromBody] FlightSearchRequestDto request, CancellationToken ct)
    {
        // [ApiController] already returns 400 for model-binding failures (e.g. an invalid
        // CabinClass string) and for [Range]/[Required] violations declared on the DTO —
        // challenge.md §3.1 requires passengers 1-9, enforced there rather than duplicated
        // here (docs/03-execution-plan.md Phase 5, task 4).
        var response = await flightSearchService.SearchAsync(request, ct);
        return Ok(response);
    }
}
