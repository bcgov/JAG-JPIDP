import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';

import { SharedModule } from '@app/shared/shared.module';

import { AccessModule } from '../../../access.module';
import { DigitalEvidenceCaseManagementInfoDialogComponent } from './digital-evidence-case-management-info-dialog';
import { DigitalEvidenceCaseManagementRoutingModule } from './digital-evidence-case-management-routing.module';
import { DigitalEvidenceCaseManagementPage } from './digital-evidence-case-management.page';

@NgModule({
  declarations: [
    DigitalEvidenceCaseManagementPage,
    DigitalEvidenceCaseManagementInfoDialogComponent,
  ],
  imports: [
    DigitalEvidenceCaseManagementRoutingModule,
    SharedModule,
    AccessModule,
    CommonModule,
    MatSortModule,
    MatPaginatorModule,
    FlexLayoutModule,
  ],
})
export class DigitalEvidenceCaseManagementModule {}
