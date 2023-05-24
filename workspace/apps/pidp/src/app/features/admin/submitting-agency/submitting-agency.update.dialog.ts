import { Component, Inject, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import {
  MatDatepicker,
  MatDatepickerInputEvent,
} from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { SubmittingAgency } from './submitting-agency.model';

@Component({
  selector: 'app-submitting-agency-update-dialog',
  templateUrl: 'submitting-agency.update.dialog.html',
  styleUrls: ['./submitting-agency.scss'],
})
export class SubmittingAgencyUpdateDialogComponent {
  public certDate: any;
  public loa = [];
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  constructor(
    public dialogRef: MatDialogRef<SubmittingAgencyUpdateDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: SubmittingAgency
  ) {
    this.certDate = new FormControl();
  }

  public updateExpiry(event: MatDatepickerInputEvent<Date>): void {
    if (event.value != null) this.data.clientCertExpiry = event.value;
  }
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  onNoClick(): void {
    this.dialogRef.close();
  }
}
