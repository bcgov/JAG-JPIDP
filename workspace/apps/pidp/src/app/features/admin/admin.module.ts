import { NgModule } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';



import { DashboardModule } from '@bcgov/shared/ui';



import { LookupModule } from '@app/modules/lookup/lookup.module';
import { SharedModule } from '@app/shared/shared.module';



import { AdminRoutingModule } from './admin-routing.module';
import { IdentityProviderComponent } from './idp/idp.component';
import { PartiesPage } from './pages/parties/parties.page';
import { PartyComponent } from './party/party.component';
import { AdminDashboardComponent } from './shared/components/admin-dashboard/admin-dashboard.component';


@NgModule({
  declarations: [
    AdminDashboardComponent,
    PartiesPage,
    PartyComponent,
    IdentityProviderComponent,
  ],
  imports: [
    AdminRoutingModule,
    DashboardModule,
    SharedModule,
    LookupModule.forChild(),
    MatChipsModule
  ],
})
export class AdminModule {}
