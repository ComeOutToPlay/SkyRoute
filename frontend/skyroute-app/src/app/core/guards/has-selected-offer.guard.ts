import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SearchState } from '../state/search-state';

export const hasSelectedOfferGuard: CanActivateFn = () => {
  const searchState = inject(SearchState);
  const router = inject(Router);

  if (searchState.selectedOfferId() || searchState.searchId()) {
    return true;
  }

  return router.createUrlTree(['/search']);
};
