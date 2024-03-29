import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { SignedOrAcceptedDocumentsPage } from './signed-or-accepted-documents.page';

const routes: Routes = [
  {
    path: '',
    component: SignedOrAcceptedDocumentsPage,
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
export class SignedOrAcceptedDocumentsRoutingModule {}
