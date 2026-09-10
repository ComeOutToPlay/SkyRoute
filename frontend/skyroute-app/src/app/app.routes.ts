import { Routes } from '@angular/router';
import { Component } from '@angular/core';
import { SearchFormComponent } from './features/search/search-form';
import { ResultsListComponent } from './features/results/results-list';

@Component({
	template: '<p>Booking flow is implemented in Phase 7.</p>',
})
class BookingPlaceholderComponent {}

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'search' },
	{ path: 'search', component: SearchFormComponent },
	{ path: 'results', component: ResultsListComponent },
	{ path: 'booking/:flightId', component: BookingPlaceholderComponent },
];
