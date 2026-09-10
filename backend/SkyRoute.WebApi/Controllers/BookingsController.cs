using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Services;

namespace SkyRoute.WebApi.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Book([FromBody] BookingRequestDto request)
    {
        var response = await bookingService.BookAsync(request);
        return Ok(response);
    }
}
