import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { PermissionsGuard } from '@app/modules/permissions/permissions.guard';
import { Role } from '@app/shared/enums/roles.enum';

import { AccessRoutes } from './access.routes';
import { DigitalEvidenceCaseManagementModule } from './pages/digital-evidence/case-management/digital-evidence-case-management.module';
import { DigitalEvidenceCounselModule } from './pages/digital-evidence/digital-evidence-counsel/digital-evidence-counsel.module';
import { DigitalEvidenceModule } from './pages/digital-evidence/digital-evidence.module';
import { DriverFitnessModule } from './pages/driver-fitness/driver-fitness.module';
import { HcimAccountTransferModule } from './pages/hcim-account-transfer/hcim-account-transfer.module';
import { HcimEnrolmentModule } from './pages/hcim-enrolment/hcim-enrolment.module';
import { JamPorModule } from './pages/jam-por/jam-por.module';
import { MsTeamsModule } from './pages/ms-teams/ms-teams.module';
import { PharmanetModule } from './pages/pharmanet/pharmanet.module';
import { SaEformsModule } from './pages/sa-eforms/sa-eforms.module';
import { SitePrivacySecurityChecklistModule } from './pages/site-privacy-security-checklist/site-privacy-security-checklist.module';
import { UciModule } from './pages/uci/uci.module';

const routes: Routes = [
  {
    path: AccessRoutes.SPECIAL_AUTH_EFORMS,
    loadChildren: (): Promise<SaEformsModule> =>
      import('./pages/sa-eforms/sa-eforms-routing.module').then(
        (m) => m.SaEformsRoutingModule
      ),
  },
  {
    path: AccessRoutes.HCIM_ACCOUNT_TRANSFER,
    loadChildren: (): Promise<HcimAccountTransferModule> =>
      import('./pages/hcim-account-transfer/hcim-account-transfer.module').then(
        (m) => m.HcimAccountTransferModule
      ),
  },
  {
    path: AccessRoutes.HCIM_ENROLMENT,
    canActivate: [PermissionsGuard],
    data: {
      roles: [Role.FEATURE_PIDP_DEMO],
    },
    loadChildren: (): Promise<HcimEnrolmentModule> =>
      import('./pages/hcim-enrolment/hcim-enrolment.module').then(
        (m) => m.HcimEnrolmentModule
      ),
  },
  {
    path: AccessRoutes.PHARMANET,
    canActivate: [PermissionsGuard],
    data: {
      roles: [Role.FEATURE_PIDP_DEMO],
    },
    loadChildren: (): Promise<PharmanetModule> =>
      import('./pages/pharmanet/pharmanet.module').then(
        (m) => m.PharmanetModule
      ),
  },
  {
    path: AccessRoutes.SITE_PRIVACY_SECURITY_CHECKLIST,
    canActivate: [PermissionsGuard],
    data: {
      roles: [Role.FEATURE_PIDP_DEMO],
    },
    loadChildren: (): Promise<SitePrivacySecurityChecklistModule> =>
      import(
        './pages/site-privacy-security-checklist/site-privacy-security-checklist.module'
      ).then((m) => m.SitePrivacySecurityChecklistModule),
  },
  {
    path: AccessRoutes.DRIVER_FITNESS,
    // canActivate: [PermissionsGuard],
    // data: {
    //   roles: [Role.FEATURE_PIDP_DEMO],
    // },
    loadChildren: (): Promise<DriverFitnessModule> =>
      import('./pages/driver-fitness/driver-fitness.module').then(
        (m) => m.DriverFitnessModule
      ),
  },
  {
    path: AccessRoutes.DIGITAL_EVIDENCE,
    //canActivate: [PermissionsGuard],
    // data: {
    //   roles: [Role.FEATURE_PIDP_DEMO],
    // },
    loadChildren: (): Promise<DigitalEvidenceModule> =>
      import('./pages/digital-evidence/digital-evidence.module').then(
        (m) => m.DigitalEvidenceModule
      ),
  },
  {
    path: AccessRoutes.JAM_POR,

    loadChildren: (): Promise<JamPorModule> =>
      import('./pages/jam-por/jam-por.module').then((m) => m.JamPorModule),
  },
  {
    path: AccessRoutes.DIGITAL_EVIDENCE_CASE_MANAGEMENT,
    //canActivate: [PermissionsGuard],
    // data: {
    //   roles: [Role.FEATURE_PIDP_DEMO],
    // },
    loadChildren: (): Promise<DigitalEvidenceCaseManagementModule> =>
      import(
        './pages/digital-evidence/case-management/digital-evidence-case-management.module'
      ).then((m) => m.DigitalEvidenceCaseManagementModule),
  },
  {
    path: AccessRoutes.DIGITAL_EVIDENCE_COUNSEL,
    //canActivate: [PermissionsGuard],
    // data: {
    //   roles: [Role.FEATURE_PIDP_DEMO],
    // },
    loadChildren: (): Promise<DigitalEvidenceCounselModule> =>
      import(
        './pages/digital-evidence/digital-evidence-counsel/digital-evidence-counsel.module'
      ).then((m) => m.DigitalEvidenceCounselModule),
  },
  {
    path: AccessRoutes.UCI,
    canActivate: [PermissionsGuard],
    data: {
      roles: [Role.FEATURE_PIDP_DEMO],
    },
    loadChildren: (): Promise<UciModule> =>
      import('./pages/uci/uci.module').then((m) => m.UciModule),
  },
  {
    path: AccessRoutes.MS_TEAMS,
    canActivate: [PermissionsGuard],
    data: {
      roles: [Role.FEATURE_PIDP_DEMO],
    },
    loadChildren: (): Promise<MsTeamsModule> =>
      import('./pages/ms-teams/ms-teams.module').then((m) => m.MsTeamsModule),
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AccessRoutingModule {}
