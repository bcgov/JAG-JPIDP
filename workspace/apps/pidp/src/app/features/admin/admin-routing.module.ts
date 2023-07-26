import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminGuard } from '@app/core/guards/admin.guard';

import { AuthRoutes } from '../auth/auth.routes';
import { AuthenticationGuard } from '../auth/guards/authentication.guard';
import { AdminRoutes } from './admin.routes';
import { CourtLocationComponent } from './courts/court.component';
import { PartiesPage } from './pages/parties/parties.page';
import { PartyComponent } from './party/party.component';
import { AdminDashboardComponent } from './shared/components/admin-dashboard/admin-dashboard.component';
import { SubmittingAgenciesComponent } from './submitting-agency/submitting-agency.page';

const routes: Routes = [
  {
    path: '',
    component: AdminDashboardComponent,
    canActivate: [AuthenticationGuard, AdminGuard],
    canActivateChild: [AuthenticationGuard],
    data: {
      routes: {
        auth: AuthRoutes.routePath(AuthRoutes.ADMIN_LOGIN),
      },
    },
    children: [
      {
        path: AdminRoutes.PARTIES,
        component: PartiesPage,
        data: { title: 'Digital Identity Access Management Admin' },
      },
      {
        path: `${AdminRoutes.PARTY}/:partyID`,
        component: PartyComponent,
        data: { title: 'Party Information' },
      },
      {
        path: AdminRoutes.SUBMITTING_AGENCY,
        component: SubmittingAgenciesComponent,
        data: { title: 'Submitting Agencies' },
      },
      {
        path: AdminRoutes.COURT_LOCATION,
        component: CourtLocationComponent,
        data: { title: 'Court Locations' },
      },
      {
        path: '',
        redirectTo: AdminRoutes.PARTIES,
        pathMatch: 'full',
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
