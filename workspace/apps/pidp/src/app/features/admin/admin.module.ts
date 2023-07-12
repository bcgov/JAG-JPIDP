import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { DashboardModule } from '@bcgov/shared/ui';

import { LookupModule } from '@app/modules/lookup/lookup.module';
import { SharedModule } from '@app/shared/shared.module';

import { AdminRoutingModule } from './admin-routing.module';
import { CourtLocationUpdateDialogComponent } from './courts/court-location.update-dialog';
import { CourtLocationComponent } from './courts/court.component';
import { IdentityProviderComponent } from './idp/idp.component';
import { PartiesPage } from './pages/parties/parties.page';
import { PartyComponent } from './party/party.component';
import { AdminDashboardComponent } from './shared/components/admin-dashboard/admin-dashboard.component';
import { SubmittingAgenciesComponent } from './submitting-agency/submitting-agency.page';
import { SubmittingAgencyUpdateDialogComponent } from './submitting-agency/submitting-agency.update.dialog';

@NgModule({
  declarations: [
    AdminDashboardComponent,
    PartiesPage,
    PartyComponent,
    IdentityProviderComponent,
    SubmittingAgenciesComponent,
    CourtLocationComponent,
    SubmittingAgencyUpdateDialogComponent,
    CourtLocationUpdateDialogComponent,
  ],

  imports: [
    AdminRoutingModule,
    DashboardModule,
    SharedModule,
    LookupModule.forChild(),
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatNativeDateModule,
    MatDatepickerModule,
  ],
})
export class AdminModule {}
