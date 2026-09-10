import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BookingResponse } from '../../core/models/booking';
import { DurationPipe } from '../../shared/pipes/duration';

@Component({
  selector: 'app-confirmation',
  imports: [CommonModule, RouterLink, DurationPipe],
  templateUrl: './confirmation.html',
  styleUrl: './confirmation.scss',
})
export class ConfirmationComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly navigationState =
    (this.router.getCurrentNavigation()?.extras.state?.['booking'] as BookingResponse | undefined) ??
    (history.state?.booking as BookingResponse | undefined);

  protected readonly bookingReference =
    this.route.snapshot.paramMap.get('bookingReference') ?? '';
  protected readonly booking = computed(() => this.navigationState ?? null);
}
