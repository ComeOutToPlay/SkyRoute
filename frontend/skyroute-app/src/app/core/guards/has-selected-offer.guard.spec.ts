import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { hasSelectedOfferGuard } from './has-selected-offer.guard';
import { SearchState } from '../state/search-state';

describe('hasSelectedOfferGuard', () => {
  it('redirects to /search when no offer is selected', () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), SearchState],
    });

    const searchState = TestBed.inject(SearchState);
    searchState.searchId.set(null);
    searchState.selectedOfferId.set(null);

    const result = TestBed.runInInjectionContext(() => hasSelectedOfferGuard({} as never, {} as never));

    expect(result instanceof UrlTree).toBe(true);
    const router = TestBed.inject(Router);
    expect(router.serializeUrl(result as UrlTree)).toBe('/search');
  });
});
