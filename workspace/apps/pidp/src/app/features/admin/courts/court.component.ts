import { AfterViewInit, Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
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
  public displayedColumns: string[] = ['name', 'code', 'active'];
  public showInactive = false;

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
      .getCourtLocations()
      .subscribe((results: CourtLocation[]) => {
        this.dataSource.data = results.sort((a, b) =>
          a.name.localeCompare(b.name)
        );
      });
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
