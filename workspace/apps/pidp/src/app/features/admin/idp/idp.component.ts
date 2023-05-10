import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';

import { IdentityProvider } from './idp.model';

@Component({
  selector: 'app-admin-idp',
  templateUrl: './idp.component.html',
})
export class IdentityProviderComponent implements OnInit {
  public providers: IdentityProvider[] | undefined;
  public dataSource!: MatTableDataSource<IdentityProvider>;
  public displayedColumns: string[] = [
    'name',
    'alias',
    'displayName',
    'enabled',
  ];

  public ngOnInit(): void {
    this.providers = [];
    this.dataSource = new MatTableDataSource(this.providers);
  }
}
