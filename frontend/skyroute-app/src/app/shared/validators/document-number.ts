import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const PASSPORT_PATTERN = /^[A-Z]{1,2}[0-9]{6,7}$/;
const NATIONAL_ID_PATTERN = /^[0-9]{9}$/;

function normalize(value: unknown): string {
  return typeof value === 'string' ? value.trim().toUpperCase() : '';
}

export function documentNumberValidator(isInternational: boolean): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const normalizedValue = normalize(control.value);

    if (!normalizedValue) {
      return null;
    }

    if (isInternational) {
      return PASSPORT_PATTERN.test(normalizedValue)
        ? null
        : { passportFormat: true };
    }

    return NATIONAL_ID_PATTERN.test(normalizedValue)
      ? null
      : { nationalIdFormat: true };
  };
}
