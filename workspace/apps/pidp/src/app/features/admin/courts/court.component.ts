import { AfterViewInit, Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { ToastService } from '@app/core/services/toast.service';
import { CourtLocation } from '@app/features/access/pages/digital-evidence/digital-evidence-counsel/digital-evidence-counsel-model';

import { AdminResource } from '../shared/resources/admin-resource.service';
import { CourtLocationUpdateDialogComponent } from './court-location.update-dialog';

@Component({
  selector: 'app-admin-courts',
  templateUrl: './court.component.html',
  styleUrls: ['./court-location.scss'],
})
export class CourtLocationComponent implements AfterViewInit {
  public courts: CourtLocation[] | undefined;
  public dataSource!: MatTableDataSource<CourtLocation>;

  public columnDefinitions = [
    { def: 'name', nonEdt: true },
    { def: 'code', nonEdt: true },
    { def: 'active', nonEdt: true },
    { def: 'edtId', nonEdt: false },
    { def: 'status', nonEdt: false },
    { def: 'key', nonEdt: false },
  ];
  public activeOnly = false;
  public showEdtInfo = false;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private adminResource: AdminResource,
    private dialog: MatDialog,
    private toastService: ToastService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.dataSource = new MatTableDataSource();
  }

  public ngAfterViewInit(): void {
    this.courts = [];
    this.updateCourts();
  }

  public updateCourts(): void {
    this.adminResource
      .getCourtLocations(this.showEdtInfo, this.activeOnly)
      .subscribe((results: CourtLocation[]) => {
        this.dataSource.data = results.sort((a, b) =>
          a.name.localeCompare(b.name)
        );
      });
  }

  public toggleActiveOnly(event: MatSlideToggleChange): void {
    this.activeOnly = event.checked;
    this.updateCourts();
  }

  public toggleEdtInfo(event: MatSlideToggleChange): void {
    this.showEdtInfo = event.checked;
    this.updateCourts();
  }

  public getDisplayedColumns(): string[] {
    return this.columnDefinitions
      .filter((cd) => (!this.showEdtInfo ? cd.nonEdt : cd))
      .map((cd) => cd.def);
  }

  public showUpdateDialog(row: CourtLocation): void {
    const dialogRef = this.dialog.open(CourtLocationUpdateDialogComponent, {
      data: row,
    });
    dialogRef.afterClosed().subscribe((result) => {
      this.adminResource.updateCourtLocation(result).subscribe(() => {
        this.toastService.openInfoToast('Updated court location');
      });
    });
  }
}
