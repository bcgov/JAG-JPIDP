import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { OrganizationDetailsPage } from './organization-details.page';

const routes: Routes = [
  {
    path: '',
    component: OrganizationDetailsPage,
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
export class OrganizationDetailsRoutingModule {}
