import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AirportService } from '../../core/services/airport.service';
import { FlightService } from '../../core/services/flight.service';
import { CabinClass, SearchRequest } from '../../core/models/search-request';
import { SearchState } from '../../core/state/search-state';

export function originDestinationDifferentValidator(
  control: AbstractControl
): ValidationErrors | null {
  const origin = control.get('origin')?.value;
  const destination = control.get('destination')?.value;

  if (!origin || !destination) {
    return null;
  }

  return origin === destination ? { sameAirport: true } : null;
}

@Component({
  selector: 'app-search-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './search-form.html',
  styleUrl: './search-form.scss',
})
export class SearchFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly airportService = inject(AirportService);
  private readonly flightService = inject(FlightService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchState = inject(SearchState);
  protected readonly cabinClasses: CabinClass[] = ['Economy', 'Business', 'First'];

  protected readonly form = this.formBuilder.group(
    {
      origin: ['', [Validators.required]],
      destination: ['', [Validators.required]],
      departureDate: ['', [Validators.required]],
      passengers: [1, [Validators.required, Validators.min(1), Validators.max(9)]],
      cabinClass: ['Economy' as CabinClass, [Validators.required]],
    },
    { validators: [originDestinationDifferentValidator] }
  );

  constructor() {
    this.airportService
      .loadAirports()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (airports) => {
          this.searchState.setAirports(airports);
        },
        error: () => {
          this.searchState.setError('Unable to load airports. Please try again.');
        },
      });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: SearchRequest = {
      origin: value.origin ?? '',
      destination: value.destination ?? '',
      departureDate: value.departureDate ?? '',
      passengers: value.passengers ?? 1,
      cabinClass: value.cabinClass ?? 'Economy',
    };

    this.searchState.setCriteria(request);
    this.searchState.setLoading(true);
    this.searchState.setError(null);
    this.searchState.clearSelectedOfferId();
    void this.router.navigate(['/results']);

    this.flightService
      .search(request)
      .subscribe({
        next: (response) => {
          this.searchState.setSearchResponse(
            response.searchId,
            response.flights,
            response.isInternational
          );
          this.searchState.setLoading(false);
        },
        error: () => {
          this.searchState.setSearchResponse('', [], false);
          this.searchState.setLoading(false);
          this.searchState.setError('Search failed. Please review your input and try again.');
        },
      });
  }
}
