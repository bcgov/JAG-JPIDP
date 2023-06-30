import { Component, Inject } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';

import { CourtLocation } from '@app/features/access/pages/digital-evidence/digital-evidence-counsel/digital-evidence-counsel-model';

@Component({
  selector: 'app-court-location-update-dialog',
  templateUrl: 'court-location.update.dialog.html',
  styleUrls: ['./court-location.scss'],
})
export class CourtLocationUpdateDialogComponent {
  public court: CourtLocation;
  public name: FormControl;
  public initialName: string;
  public active: boolean;
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  constructor(
    public dialogRef: MatDialogRef<CourtLocationUpdateDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CourtLocation
  ) {
    this.name = new FormControl();
    this.court = data;
    this.initialName = data.name;
    this.active = true;
  }

  public toggleInactive(event: MatSlideToggleChange): void {
    this.court.active = event.checked;
  }
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  onNoClick(): void {
    this.dialogRef.close();
  }
}
