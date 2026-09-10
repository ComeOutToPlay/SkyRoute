using System.Collections.Concurrent;
using SkyRoute.Application.Abstractions;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Data;

public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly ConcurrentDictionary<string, Booking> _bookings =
        new(StringComparer.OrdinalIgnoreCase);

    public void Save(Booking booking)
    {
        if (!_bookings.TryAdd(booking.Reference, booking))
        {
            throw new InvalidOperationException($"Booking reference '{booking.Reference}' already exists.");
        }
    }

    public Booking? FindByReference(string reference) =>
        _bookings.TryGetValue(reference, out var booking) ? booking : null;
}
