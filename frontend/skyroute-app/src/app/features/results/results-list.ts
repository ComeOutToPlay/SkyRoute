import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SearchState } from '../../core/state/search-state';
import { DurationPipe } from '../../shared/pipes/duration';
import { SortToolbarComponent } from './sort-toolbar';
import { EmptyStateComponent } from './empty-state';

@Component({
  selector: 'app-results-list',
  imports: [CommonModule, DurationPipe, SortToolbarComponent, EmptyStateComponent],
  templateUrl: './results-list.html',
  styleUrl: './results-list.scss',
})
export class ResultsListComponent {
  private readonly router = inject(Router);
  protected readonly searchState = inject(SearchState);

  protected openBooking(flightId: string): void {
    this.searchState.setSelectedOfferId(flightId);
    void this.router.navigate(['/booking', flightId]);
  }
}
