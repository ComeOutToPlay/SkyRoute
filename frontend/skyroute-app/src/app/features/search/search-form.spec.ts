import { FormControl, FormGroup, Validators } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { originDestinationDifferentValidator } from './search-form';

describe('search form validators', () => {
  it('rejects origin equal to destination', () => {
    const form = new FormGroup(
      {
        origin: new FormControl('JFK', { nonNullable: true, validators: [Validators.required] }),
        destination: new FormControl('JFK', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        departureDate: new FormControl('2026-01-01', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        passengers: new FormControl(1, {
          nonNullable: true,
          validators: [Validators.required, Validators.min(1), Validators.max(9)],
        }),
        cabinClass: new FormControl('Economy', {
          nonNullable: true,
          validators: [Validators.required],
        }),
      },
      { validators: [originDestinationDifferentValidator] }
    );

    expect(form.hasError('sameAirport')).toBe(true);
  });

  it('rejects passenger count outside 1 to 9', () => {
    const passengers = new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(9)],
    });

    expect(passengers.invalid).toBe(true);
    passengers.setValue(10);
    expect(passengers.invalid).toBe(true);
    passengers.setValue(4);
    expect(passengers.valid).toBe(true);
  });
});
