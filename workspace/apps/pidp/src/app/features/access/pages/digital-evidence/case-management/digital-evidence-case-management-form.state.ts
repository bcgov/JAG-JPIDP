import { FormBuilder, FormControl } from '@angular/forms';

import { AbstractFormState, FormControlValidators } from '@bcgov/shared/ui';

import { DemsAccount } from '../digital-evidence-account.model';
import { DigitalEvidenceCase } from './digital-evidence-case.model';

export class DigitalEvidenceCaseManagementFormState extends AbstractFormState<DemsAccount> {
  public constructor(private fb: FormBuilder) {
    super();

    this.buildForm();
  }
  public get OrganizationType(): FormControl {
    return this.formInstance.get('OrganizationType') as FormControl;
  }
  public get OrganizationName(): FormControl {
    return this.formInstance.get('OrganizationName') as FormControl;
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
      agencyCode: [null, [FormControlValidators.requiredBoolean]],
      caseName: [null, [FormControlValidators.alphanumeric]],
    });
  }
}
