import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { JamPorResolver } from './jam-por.resolver';
import { JamPorPage } from './jampor-page';

const routes: Routes = [
  {
    path: '',
    component: JamPorPage,
    resolve: {
      jamPorStatusCode: JamPorResolver,
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
export class JamPorRoutingModule {}
