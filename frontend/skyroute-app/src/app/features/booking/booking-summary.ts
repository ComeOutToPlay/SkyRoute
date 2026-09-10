import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { SearchRequest } from '../../core/models/search-request';
import { FlightOfferView } from '../../core/models/search-response';
import { DurationPipe } from '../../shared/pipes/duration';

@Component({
  selector: 'app-booking-summary',
  imports: [CommonModule, DurationPipe],
  templateUrl: './booking-summary.html',
  styleUrl: './booking-summary.scss',
})
export class BookingSummaryComponent {
  readonly offer = input<FlightOfferView | null>(null);
  readonly criteria = input<SearchRequest | null>(null);

  protected readonly hasData = computed(() => !!this.offer() && !!this.criteria());
}
