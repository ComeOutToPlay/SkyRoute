import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { SearchState, SortMode } from '../../core/state/search-state';

@Component({
  selector: 'app-sort-toolbar',
  imports: [CommonModule],
  templateUrl: './sort-toolbar.html',
  styleUrl: './sort-toolbar.scss',
})
export class SortToolbarComponent {
  protected readonly searchState = inject(SearchState);

  protected readonly modes: Array<{ label: string; value: SortMode }> = [
    { label: 'Price ↑', value: 'price-asc' },
    { label: 'Price ↓', value: 'price-desc' },
    { label: 'Duration (shortest first)', value: 'duration-asc' },
    { label: 'Departure time', value: 'departure-asc' },
  ];

  protected setSortMode(mode: SortMode): void {
    this.searchState.setSortMode(mode);
  }
}
