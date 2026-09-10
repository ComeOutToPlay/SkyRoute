import { Injectable, computed, signal } from '@angular/core';
import { Airport } from '../models/airport';
import { SearchRequest } from '../models/search-request';
import { FlightOfferView } from '../models/search-response';

export type SortMode =
  | 'price-asc'
  | 'price-desc'
  | 'duration-asc'
  | 'departure-asc';

export function sortFlightOffers(
  offers: FlightOfferView[],
  sortMode: SortMode
): FlightOfferView[] {
  const sortedOffers = [...offers];

  switch (sortMode) {
    case 'price-asc':
      return sortedOffers.sort((a, b) => a.totalPrice - b.totalPrice);
    case 'price-desc':
      return sortedOffers.sort((a, b) => b.totalPrice - a.totalPrice);
    case 'duration-asc':
      return sortedOffers.sort((a, b) => a.durationMinutes - b.durationMinutes);
    case 'departure-asc':
      return sortedOffers.sort(
        (a, b) =>
          new Date(a.departureTime).getTime() - new Date(b.departureTime).getTime()
      );
    default:
      return sortedOffers;
  }
}

@Injectable({ providedIn: 'root' })
export class SearchState {
  readonly airports = signal<Airport[]>([]);
  readonly criteria = signal<SearchRequest | null>(null);
  readonly searchId = signal<string | null>(null);
  readonly results = signal<FlightOfferView[]>([]);
  readonly isInternational = signal(false);
  readonly sortMode = signal<SortMode>('price-asc');
  readonly selectedOfferId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly sortedResults = computed(() =>
    sortFlightOffers(this.results(), this.sortMode())
  );

  setAirports(airports: Airport[]): void {
    this.airports.set(airports);
  }

  setCriteria(criteria: SearchRequest): void {
    this.criteria.set(criteria);
  }

  setSearchResponse(searchId: string, offers: FlightOfferView[], isInternational: boolean): void {
    this.searchId.set(searchId);
    this.results.set(offers);
    this.isInternational.set(isInternational);
  }

  setSortMode(sortMode: SortMode): void {
    this.sortMode.set(sortMode);
  }

  setSelectedOfferId(flightId: string): void {
    this.selectedOfferId.set(flightId);
  }

  clearSelectedOfferId(): void {
    this.selectedOfferId.set(null);
  }

  setLoading(isLoading: boolean): void {
    this.loading.set(isLoading);
  }

  setError(errorMessage: string | null): void {
    this.error.set(errorMessage);
  }
}
