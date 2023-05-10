import { Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, exhaustMap } from 'rxjs';

import {
  ConfirmDialogComponent,
  DialogOptions,
  HtmlComponent,
} from '@bcgov/shared/ui';

import { APP_CONFIG, AppConfig } from '@app/app.config';

import { EnvironmentName } from '../../../../../environments/environment.model';
import {
  AdminResource,
  PartyList,
} from '../../shared/resources/admin-resource.service';

@Component({
  selector: 'app-parties',
  templateUrl: './parties.page.html',
  styleUrls: ['./parties.page.scss'],
})
export class PartiesPage implements OnInit {
  public title: string;
  public dataSource: MatTableDataSource<PartyList>;
  public displayedColumns: string[] = [
    'id',
    'providerName',
    'providerOrganizationCode',
    'organizationName',
    'digitalEvidenceAccessRequest',
  ];
  public environment: string;
  public production: string;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private adminResource: AdminResource,
    private dialog: MatDialog,
    private router: Router,
    private route: ActivatedRoute
  ) {
    const routeData = this.route.snapshot.data;
    this.title = routeData.title;
    this.title = route.snapshot.data.title;
    this.dataSource = new MatTableDataSource();
    this.environment = this.config.environmentName;
    this.production = EnvironmentName.PRODUCTION;
  }

  public ngOnInit(): void {
    this.getParties();
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public showUser(user: PartyList): void {
    this.router.navigateByUrl('/admin/party/' + user.id);
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }

  private getParties(): void {
    this.adminResource
      .getParties()
      .subscribe(
        (parties: PartyList[]) =>
          (this.dataSource.data = parties.sort((a, b) => a.id - b.id))
      );
  }
}
