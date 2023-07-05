import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { CollegeLicenceInformationPage } from './college-licence-information.page';

const routes: Routes = [
  {
    path: '',
    component: CollegeLicenceInformationPage,
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
export class CollegeLicenceInformationRoutingModule {}
