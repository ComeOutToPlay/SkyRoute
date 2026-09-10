import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { documentNumberValidator } from './document-number';

describe('documentNumberValidator', () => {
  it('accepts and rejects passport format for international routes', () => {
    const control = new FormControl('X1234567');
    const validator = documentNumberValidator(true);

    expect(validator(control)).toBeNull();

    control.setValue('123456789');
    expect(validator(control)).toEqual({ passportFormat: true });
  });

  it('accepts and rejects national ID format for domestic routes', () => {
    const control = new FormControl('123456789');
    const validator = documentNumberValidator(false);

    expect(validator(control)).toBeNull();

    control.setValue('X1234567');
    expect(validator(control)).toEqual({ nationalIdFormat: true });
  });

  it('switches active rule based on isInternational', () => {
    const control = new FormControl('X1234567');

    expect(documentNumberValidator(true)(control)).toBeNull();
    expect(documentNumberValidator(false)(control)).toEqual({ nationalIdFormat: true });
  });
});
