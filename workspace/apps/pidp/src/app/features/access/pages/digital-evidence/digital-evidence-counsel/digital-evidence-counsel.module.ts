import { NgModule } from '@angular/core';

import { SharedModule } from '@app/shared/shared.module';

import { DigitalEvidenceCounselRoutingModule } from './digital-evidence-counsel-routing.module';
import { DigitalEvidenceCounselPage } from './digital-evidence-counsel.page';

@NgModule({
  declarations: [DigitalEvidenceCounselPage],
  imports: [DigitalEvidenceCounselRoutingModule, SharedModule],
})
export class DigitalEvidenceCounselModule {}
