import { FormBuilder, FormControl, Validators } from '@angular/forms';

import { AbstractFormState } from '@bcgov/shared/ui';

import { DemsAccount } from './digital-evidence-account.model';

export class JamPorFormState extends AbstractFormState<DemsAccount> {
  public constructor(private fb: FormBuilder) {
    super();

    this.buildForm();
  }

  public get json(): DemsAccount | undefined {
    if (!this.formInstance) {
      return;
    }

    return this.formInstance.getRawValue();
  }

  public get ParticipantId(): FormControl {
    return this.formInstance.get('ParticipantId') as FormControl;
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
