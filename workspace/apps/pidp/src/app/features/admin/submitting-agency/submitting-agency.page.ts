import { Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { ToastService } from '@app/core/services/toast.service';

import { AdminResource } from '../shared/resources/admin-resource.service';
import { SubmittingAgency } from './submitting-agency.model';
import { SubmittingAgencyUpdateDialogComponent } from './submitting-agency.update.dialog';

@Component({
  selector: 'app-admin-submitting-agency',
  templateUrl: './submitting-agency.page.html',
  styleUrls: ['./submitting-agency.scss'],
})
export class SubmittingAgenciesComponent implements OnInit {
  public submittingAgencies: SubmittingAgency[] | undefined;
  public dataSource: MatTableDataSource<SubmittingAgency>;
  public displayedColumns: string[] = [
    'name',
    'idpHint',
    'clientCertExpiry',
    'levelOfAssurance',
  ];

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

  public ngOnInit(): void {
    this.submittingAgencies = [];
    this.adminResource
      .getSubmittingAgencies()
      .subscribe((agencies: SubmittingAgency[]) => {
        this.dataSource.data = agencies.sort((a, b) =>
          a.name.localeCompare(b.name)
        );
      });
  }

  public showUpdateDialog(row: SubmittingAgency): void {
    const dialogRef = this.dialog.open(SubmittingAgencyUpdateDialogComponent, {
      data: row,
    });
    dialogRef.afterClosed().subscribe((result) => {
      this.adminResource.updateSubmittingAgency(result).subscribe(() => {
        alert('Updated');
      });
    });
  }
}
