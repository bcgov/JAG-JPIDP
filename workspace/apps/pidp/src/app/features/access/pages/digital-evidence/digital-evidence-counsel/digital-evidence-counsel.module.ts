import { NgModule } from '@angular/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';

import { AccessModule } from '@app/features/access/access.module';
import { SharedModule } from '@app/shared/shared.module';

import { DigitalEvidenceCounselRoutingModule } from './digital-evidence-counsel-routing.module';
import { DigitalEvidenceCounselPage } from './digital-evidence-counsel.page';

@NgModule({
  declarations: [DigitalEvidenceCounselPage],
  imports: [
    DigitalEvidenceCounselRoutingModule,
    SharedModule,
    AccessModule,
    MatSortModule,
    MatPaginatorModule,
    FlexLayoutModule,
  ],
})
export class DigitalEvidenceCounselModule {}
