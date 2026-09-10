export type CabinClass = 'Economy' | 'Business' | 'First';

export interface SearchRequest {
  origin: string;
  destination: string;
  departureDate: string;
  passengers: number;
  cabinClass: CabinClass;
}
