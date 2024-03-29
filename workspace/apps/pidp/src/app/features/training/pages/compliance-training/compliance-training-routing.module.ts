import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { ComplianceTrainingPage } from './compliance-training.page';

const routes: Routes = [
  {
    path: '',
    component: ComplianceTrainingPage,
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
export class ComplianceTrainingRoutingModule {}
