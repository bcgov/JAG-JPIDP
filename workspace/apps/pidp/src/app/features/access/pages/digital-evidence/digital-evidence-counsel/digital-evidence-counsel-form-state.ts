import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { throwToolbarMixedModesError } from '@angular/material/toolbar';

import { AbstractFormState } from '@bcgov/shared/ui';

import { DemsAccount } from '../digital-evidence-account.model';

export class DigitalEvidenceCounselFormState extends AbstractFormState<DemsAccount> {
  public constructor(private fb: FormBuilder) {
    super();
    this.buildForm();
  }

  public get courtLocation(): FormControl {
    return this.formInstance.get('courtLocation') as FormControl;
  }

  public get dateFrom(): FormControl {
    return this.formInstance.get('dateFrom') as FormControl;
  }

  public get dateTo(): FormControl {
    return this.formInstance.get('dateTo') as FormControl;
  }

  public get showDeleted(): FormControl {
    return this.formInstance.get('showDeleted') as FormControl;
  }

  public get json(): DemsAccount | undefined {
    if (!this.formInstance) {
      return;
    }

    return this.formInstance.getRawValue();
  }

  public patchValue(model: DemsAccount | null): void {
    if (!this.formInstance || !model) {
      return;
    }

    this.formInstance.patchValue(model);
  }

  public reset(): void {
    this.formInstance.reset();
  }

  public buildForm(): void {
    this.formInstance = this.fb.group({
      courtLocation: ['', [Validators.required]],
      dateFrom: ['', [Validators.required]],
      dateTo: ['', [Validators.required]],
      showDeleted: [''],
    });
  }
}
