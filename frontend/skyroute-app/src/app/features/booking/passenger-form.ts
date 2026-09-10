import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { BookingService, BookingApiError } from '../../core/services/booking.service';
import { SearchState } from '../../core/state/search-state';
import { documentNumberValidator } from '../../shared/validators/document-number';
import { BookingRequest, PassengerFormValue } from '../../core/models/booking';
import { BookingSummaryComponent } from './booking-summary';

@Component({
  selector: 'app-passenger-form',
  imports: [CommonModule, ReactiveFormsModule, BookingSummaryComponent],
  templateUrl: './passenger-form.html',
  styleUrl: './passenger-form.scss',
})
export class PassengerFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly bookingService = inject(BookingService);
  protected readonly searchState = inject(SearchState);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isSubmitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly offerExpired = signal(false);
  protected readonly documentLabel = computed(() =>
    this.searchState.isInternational() ? 'Passport Number' : 'National ID'
  );
  protected readonly passengerCount = computed(
    () => this.searchState.criteria()?.passengers ?? 0
  );

  protected readonly selectedOffer = computed(() => {
    const routeFlightId = this.route.snapshot.paramMap.get('flightId');
    const selectedId = this.searchState.selectedOfferId() ?? routeFlightId;
    if (!selectedId) {
      return null;
    }

    return this.searchState.results().find((offer) => offer.id === selectedId) ?? null;
  });

  protected readonly form = this.formBuilder.group({
    passengers: this.formBuilder.array<FormGroup>([]),
  });

  constructor() {
    const routeFlightId = this.route.snapshot.paramMap.get('flightId');
    if (routeFlightId && !this.searchState.selectedOfferId()) {
      this.searchState.setSelectedOfferId(routeFlightId);
    }

    this.buildPassengerRows();
  }

  protected get passengersArray(): FormArray<FormGroup> {
    return this.form.controls.passengers;
  }

  protected get passengerRows(): FormGroup[] {
    return this.passengersArray.controls;
  }

  protected confirmBooking(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const selectedOffer = this.selectedOffer();
    const searchId = this.searchState.searchId();
    if (!selectedOffer || !searchId) {
      this.error.set('Selected flight information is missing. Please search again.');
      return;
    }

    const request: BookingRequest = {
      searchId,
      flightId: selectedOffer.id,
      passengers: this.passengersArray.getRawValue() as PassengerFormValue[],
    };

    this.isSubmitting.set(true);
    this.offerExpired.set(false);
    this.error.set(null);

    this.bookingService
      .book(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/booking/confirmation', response.bookingReference], {
            state: { booking: response },
          });
        },
        error: (bookingError: unknown) => {
          this.isSubmitting.set(false);

          if (bookingError instanceof BookingApiError && bookingError.isOfferExpired) {
            this.offerExpired.set(true);
            this.error.set('Fares have changed, please search again.');
            return;
          }

          this.error.set('Booking failed. Please try again.');
        },
      });
  }

  protected backToSearch(): void {
    void this.router.navigate(['/search']);
  }

  private buildPassengerRows(): void {
    const count = this.passengerCount();
    for (let i = 0; i < count; i += 1) {
      this.passengersArray.push(
        this.formBuilder.group({
          fullName: ['', [Validators.required]],
          email: ['', [Validators.required, Validators.email]],
          documentNumber: [
            '',
            [Validators.required, documentNumberValidator(this.searchState.isInternational())],
          ],
        })
      );
    }
  }
}
