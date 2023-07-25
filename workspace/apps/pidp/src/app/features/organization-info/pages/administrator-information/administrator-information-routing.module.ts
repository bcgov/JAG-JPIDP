import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdministratorInformationPage } from './administrator-information.page';

const routes: Routes = [
  {
    path: '',
    component: AdministratorInformationPage,
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
export class AdministratorInformationRoutingModule {}
