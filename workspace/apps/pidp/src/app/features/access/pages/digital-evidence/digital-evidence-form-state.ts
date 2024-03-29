import { FormBuilder, FormControl, Validators } from '@angular/forms';

import { AbstractFormState } from '@bcgov/shared/ui';

import { DemsAccount } from './digital-evidence-account.model';

export class DigitalEvidenceFormState extends AbstractFormState<DemsAccount> {
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

  public get OOCUniqueId(): FormControl {
    return this.formInstance.get('OOCUniqueId') as FormControl;
  }

  public get DefenceUniqueId(): FormControl {
    return this.formInstance.get('DefenceUniqueId') as FormControl;
  }

  public get DefenceUniqueIdValid(): FormControl {
    return this.formInstance.get('DefenceUniqueIdValid') as FormControl;
  }

  public get OOCUniqueIdValid(): FormControl {
    return this.formInstance.get('OOCUniqueIdValid') as FormControl;
  }

  public get ParticipantId(): FormControl {
    return this.formInstance.get('ParticipantId') as FormControl;
  }
  public get AssignedRegions(): FormControl {
    return this.formInstance.get('AssignedRegions') as FormControl;
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

  public buildForm(): void {
    this.formInstance = this.fb.group({
      OrganizationType: ['', [Validators.required]],
      OrganizationName: ['', [Validators.required]],
      ParticipantId: ['', [Validators.required]],
      OOCUniqueId: [],
      OOCUniqueIdValid: [],

      // DefenceUniqueId: [
      //   '',
      //   [Validators.pattern('^[A-Za-z]{2,3}-[0-9]{6}$'), Validators.required],
      // ],
      // Conditionally include AssignedRegions if OrganizationType is set to a specific value
      AssignedRegions: [],
    });
  }
}
