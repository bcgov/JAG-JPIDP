import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { MsTeamsPage } from './ms-teams.page';
import { MsTeamsResolver } from './ms-teams.resolver';

const routes: Routes = [
  {
    path: '',
    component: MsTeamsPage,
    resolve: {
      msTeamsStatusCode: MsTeamsResolver,
    },
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
export class MsTeamsRoutingModule {}
