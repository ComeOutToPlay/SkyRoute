using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Abstractions;
using SkyRoute.Application.Dtos;

namespace SkyRoute.WebApi.Controllers;

[ApiController]
[Route("api/airports")]
public sealed class AirportsController(IAirportCatalog airportCatalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AirportDto>> GetAll()
    {
        var airports = airportCatalog.GetAll()
            .Select(a => new AirportDto(a.Code, a.City, a.Country, a.CountryCode))
            .ToList();

        return Ok(airports);
    }
}
