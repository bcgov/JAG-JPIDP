import { NgModule } from '@angular/core';

import { SharedModule } from '@app/shared/shared.module';

import { AccessModule } from '../../../access.module';
import { DigitalEvidenceCaseManagementRoutingModule } from './digital-evidence-case-management-routing.module';
import { DigitalEvidenceCaseManagementPage } from './digital-evidence-case-management.page';

@NgModule({
  declarations: [DigitalEvidenceCaseManagementPage],
  imports: [
    DigitalEvidenceCaseManagementRoutingModule,
    SharedModule,
    AccessModule,
  ],
})
export class DigitalEvidenceCaseManagementModule {}
