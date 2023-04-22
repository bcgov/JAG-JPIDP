import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { DigitalEvidenceCounselPage } from './digital-evidence-counsel.page';
import { DigitalEvidenceCounselResolver } from './digital-evidence-counsel.resolver';

const routes: Routes = [
  {
    path: '',
    component: DigitalEvidenceCounselPage,
    resolve: {
      digitalEvidenceCounselStatusCode: DigitalEvidenceCounselResolver,
    },
    data: {
      title: 'JPS Provider Identity Portal',
      routes: {
        root: '../../',
      },
    },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DigitalEvidenceCounselRoutingModule {}
