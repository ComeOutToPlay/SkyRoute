import { CabinClass } from './search-request';

export interface FlightOfferView {
  id: string;
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  durationMinutes: number;
  cabinClass: CabinClass;
  pricePerPassenger: number;
  totalPrice: number;
}

export interface SearchResponse {
  searchId: string;
  passengerCount: number;
  currency: string;
  isInternational: boolean;
  flights: FlightOfferView[];
}
