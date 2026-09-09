using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Abstractions;

// Backed by a ConcurrentDictionary<string, Booking> in Infrastructure (Phase 8) — no database,
// per the approved scope (docs/02-revision.md: in-memory storage, no persistence).
public interface IBookingStore
{
    void Save(Booking booking);

    Booking? FindByReference(string reference);
}
