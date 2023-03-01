import { FormBuilder, FormControl, Validators } from '@angular/forms';

import { AbstractFormState, FormControlValidators } from '@bcgov/shared/ui';

import { DemsAccount } from '../digital-evidence-account.model';

export class DigitalEvidenceCaseManagementFormState extends AbstractFormState<DemsAccount> {
  public constructor(private fb: FormBuilder) {
    super();

    this.buildForm();
  }

  public get ParticipantId(): FormControl {
    return this.formInstance.get('ParticipantId') as FormControl;
  }

  public get agencyCode(): FormControl {
    return this.formInstance.get('agencyCode') as FormControl;
  }

  public get requestedCase(): FormControl {
    return this.formInstance.get('requestedCase') as FormControl;
  }

  public get caseName(): FormControl {
    return this.formInstance.get('caseName') as FormControl;
  }

  public get caseListing(): FormControl {
    return this.formInstance.get('caseListing') as FormControl;
  }

  public get json(): DemsAccount | undefined {
    if (!this.formInstance) {
      return;
    }

    return this.formInstance.getRawValue();
  }

  public patchValue(): void {
    // Form will never be patched!
    throw new Error('Method Not Implemented');
  }

  public buildForm(): void {
    this.formInstance = this.fb.group({
      agencyCode: [
        null,
        [FormControlValidators.alphanumeric, Validators.minLength(2)],
      ],
      caseName: [
        null,
        [
          FormControlValidators.alphanumeric,
          Validators.minLength(8),
          Validators.maxLength(12),
        ],
      ],
    });
  }
}
