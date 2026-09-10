import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BookingRequest, BookingResponse } from '../models/booking';
import { environment } from '../../environments/environment';

export class BookingApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly isOfferExpired: boolean
  ) {
    super(message);
    this.name = 'BookingApiError';
  }
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly httpClient = inject(HttpClient);

  book(request: BookingRequest): Observable<BookingResponse> {
    return this.httpClient
      .post<BookingResponse>(`${environment.apiUrl}/bookings`, request)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.title === 'string'
              ? error.error.title
              : 'Booking failed. Please try again.';

          return throwError(
            () => new BookingApiError(message, error.status, error.status === 409)
          );
        })
      );
  }
}
