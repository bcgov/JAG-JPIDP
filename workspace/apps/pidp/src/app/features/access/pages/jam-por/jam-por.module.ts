import { NgModule } from '@angular/core';

import { SharedModule } from '@app/shared/shared.module';

import { JamPorRoutingModule } from './jam-por-routing.module';
import { JamPorPage } from './jampor-page';

@NgModule({
  declarations: [JamPorPage],
  imports: [JamPorRoutingModule, SharedModule],
})
export class JamPorModule {}
