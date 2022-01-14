import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthRoutes } from './auth.routes';
import { AuthorizationRedirectGuard } from './guards/authorization-redirect.guard';
import { LoginComponent } from './pages/login/login.component';

const routes: Routes = [
  {
    path: AuthRoutes.LOGIN,
    canActivate: [AuthorizationRedirectGuard],
    component: LoginComponent,
    data: {
      title: 'Provider Identity Portal',
    },
  },
  {
    path: '',
    redirectTo: AuthRoutes.LOGIN,
    pathMatch: 'full',
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuthRoutingModule {}
