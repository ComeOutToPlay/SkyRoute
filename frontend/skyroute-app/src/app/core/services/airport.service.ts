import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { finalize, shareReplay, tap } from 'rxjs/operators';
import { Airport } from '../models/airport';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AirportService {
  private readonly httpClient = inject(HttpClient);
  private readonly airportList = signal<Airport[]>([]);
  private hasLoaded = false;
  private inFlightRequest: Observable<Airport[]> | null = null;

  readonly airports = this.airportList.asReadonly();

  loadAirports(): Observable<Airport[]> {
    if (this.hasLoaded) {
      return of(this.airportList());
    }

    if (this.inFlightRequest) {
      return this.inFlightRequest;
    }

    this.inFlightRequest = this.httpClient
      .get<Airport[]>(`${environment.apiUrl}/airports`)
      .pipe(
        tap((airports) => {
          this.airportList.set(airports);
          this.hasLoaded = true;
        }),
        finalize(() => {
          this.inFlightRequest = null;
        }),
        shareReplay(1)
      );

    return this.inFlightRequest;
  }
}
