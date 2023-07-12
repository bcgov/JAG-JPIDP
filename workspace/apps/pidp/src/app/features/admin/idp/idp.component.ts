import { Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { ToastService } from '@app/core/services/toast.service';

import { AdminResource } from '../shared/resources/admin-resource.service';
import { IdentityProvider } from './idp.model';

@Component({
  selector: 'app-admin-idp',
  templateUrl: './idp.component.html',
  styleUrls: ['./idp.component.scss'],
})
export class IdentityProviderComponent implements OnInit {
  public providers: IdentityProvider[] | undefined;
  public dataSource!: MatTableDataSource<IdentityProvider>;
  public displayedColumns: string[] = [
    'alias',
    'internalId',
    'enabled',
    'providerId',
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
    this.providers = [];
    this.getIdentityProviders();
  }

  private getIdentityProviders(): void {
    this.adminResource
      .getIdentityProviders()
      .subscribe((results: IdentityProvider[]) => {
        this.dataSource.data = results.sort((a, b) =>
          a.alias.localeCompare(b.alias)
        );
      });
  }
}
