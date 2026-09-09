namespace SkyRoute.Domain.Enums;

// Only "Confirmed" is produced by the challenge's booking flow today. Kept as an enum
// (rather than a bool/const) so it is trivially extensible later, e.g. "Cancelled".
public enum BookingStatus
{
    Confirmed
}
