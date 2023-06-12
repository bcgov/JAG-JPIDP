import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';



import { DigitalEvidenceCaseManagementPage } from './digital-evidence-case-management.page';
import { DigitalEvidenceCaseManagementResolver } from './digital-evidence-case-management.resolver';


const routes: Routes = [
  {
    path: '',
    component: DigitalEvidenceCaseManagementPage,
    resolve: {
      digitalEvidenceCaseManagementStatusCode:
        DigitalEvidenceCaseManagementResolver,
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
export class DigitalEvidenceCaseManagementRoutingModule {}
