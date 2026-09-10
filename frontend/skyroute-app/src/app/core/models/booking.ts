import { FlightOfferView } from './search-response';

export interface PassengerFormValue {
  fullName: string;
  email: string;
  documentNumber: string;
}

export interface BookingRequest {
  searchId: string;
  flightId: string;
  passengers: PassengerFormValue[];
}

export interface BookingResponse {
  bookingReference: string;
  status: string;
  flightSummary: FlightOfferView;
  pricePerPassenger: number;
  passengerCount: number;
  totalPrice: number;
  currency: string;
}
