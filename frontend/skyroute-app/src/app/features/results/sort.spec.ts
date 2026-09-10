import { describe, expect, it } from 'vitest';
import { FlightOfferView } from '../../core/models/search-response';
import { sortFlightOffers } from '../../core/state/search-state';

function offer(
  id: string,
  totalPrice: number,
  durationMinutes: number,
  departureTime: string
): FlightOfferView {
  return {
    id,
    provider: 'Provider',
    flightNumber: `SR${id}`,
    origin: 'JFK',
    destination: 'LAX',
    departureTime,
    arrivalTime: departureTime,
    durationMinutes,
    cabinClass: 'Economy',
    pricePerPassenger: totalPrice / 2,
    totalPrice,
  };
}

describe('sortFlightOffers', () => {
  const source = [
    offer('A', 500, 420, '2026-01-03T18:00:00'),
    offer('B', 300, 360, '2026-01-02T08:30:00'),
    offer('C', 400, 240, '2026-01-01T12:15:00'),
  ];

  it('sorts by price ascending', () => {
    const result = sortFlightOffers(source, 'price-asc');
    expect(result.map((x) => x.id)).toEqual(['B', 'C', 'A']);
  });

  it('sorts by price descending', () => {
    const result = sortFlightOffers(source, 'price-desc');
    expect(result.map((x) => x.id)).toEqual(['A', 'C', 'B']);
  });

  it('sorts by duration ascending', () => {
    const result = sortFlightOffers(source, 'duration-asc');
    expect(result.map((x) => x.id)).toEqual(['C', 'B', 'A']);
  });

  it('sorts by departure time ascending', () => {
    const result = sortFlightOffers(source, 'departure-asc');
    expect(result.map((x) => x.id)).toEqual(['C', 'B', 'A']);
  });

  it('is pure and does not mutate the source array', () => {
    const copy = [...source];
    void sortFlightOffers(source, 'price-asc');
    expect(source).toEqual(copy);
  });
});
