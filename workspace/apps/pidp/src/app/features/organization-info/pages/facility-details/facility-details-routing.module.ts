import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { FacilityDetailsPage } from './facility-details.page';

const routes: Routes = [
  {
    path: '',
    component: FacilityDetailsPage,
    data: {
      title: 'Digital Identity Access Management',
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
export class FacilityDetailsRoutingModule {}
