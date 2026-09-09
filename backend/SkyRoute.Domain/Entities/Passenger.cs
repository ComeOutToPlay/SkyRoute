namespace SkyRoute.Domain.Entities;

public sealed class Passenger
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string DocumentNumber { get; init; }
}
