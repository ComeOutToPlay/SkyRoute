import { Routes } from '@angular/router';
import { SearchFormComponent } from './features/search/search-form';
import { ResultsListComponent } from './features/results/results-list';
import { PassengerFormComponent } from './features/booking/passenger-form';
import { ConfirmationComponent } from './features/booking/confirmation';
import { hasSelectedOfferGuard } from './core/guards/has-selected-offer.guard';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'search' },
	{ path: 'search', component: SearchFormComponent },
	{ path: 'results', component: ResultsListComponent },
	{ path: 'booking/confirmation/:bookingReference', component: ConfirmationComponent },
	{
		path: 'booking/:flightId',
		component: PassengerFormComponent,
		canActivate: [hasSelectedOfferGuard],
	},
];
