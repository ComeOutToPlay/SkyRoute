using System.ComponentModel.DataAnnotations;
using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Dtos;

public sealed record FlightSearchRequestDto(
    [Required] string Origin,
    [Required] string Destination,
    DateOnly DepartureDate,
    // challenge.md §3.1: "Number of passengers (1 to 9)". [ApiController] on the controller
    // (Phase 5) turns this into an automatic 400 ProblemDetails response on violation.
    // Attribute must target the constructor parameter directly (not [property: ...]) for
    // MVC's validator to recognize it on a record's primary constructor — [property: ...]
    // throws InvalidOperationException at request-validation time on ASP.NET Core 10.
    [Range(1, 9)] int Passengers,
    CabinClass CabinClass);
