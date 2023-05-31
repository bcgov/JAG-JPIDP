import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { DigitalEvidenceCase } from './digital-evidence-case.model';

@Component({
  selector: 'app-digital-evidence-case-management-info-dialog',
  templateUrl: 'digital-evidence-case-management-info-dialog.html',
})
export class DigitalEvidenceCaseManagementInfoDialogComponent {
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  constructor(
    public dialogRef: MatDialogRef<DigitalEvidenceCaseManagementInfoDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DigitalEvidenceCase
  ) {}

  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  onNoClick(): void {
    this.dialogRef.close();
  }
}
