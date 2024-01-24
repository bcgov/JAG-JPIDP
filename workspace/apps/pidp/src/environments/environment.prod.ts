import { digitalEvidenceSupportEmail } from '@app/features/access/pages/digital-evidence/digital-evidence.constants';
import { driverFitnessSupportEmail } from '@app/features/access/pages/driver-fitness/driver-fitness.constants';
import { hcimWebAccountTransferSupport } from '@app/features/access/pages/hcim-account-transfer/hcim-account-transfer-constants';
import { hcimWebEnrolmentSupport } from '@app/features/access/pages/hcim-enrolment/hcim-enrolment-constants';
import {
  doctorsTechnologyOfficeEmail,
  doctorsTechnologyOfficeUrl,
  msTeamsSupportEmail,
} from '@app/features/access/pages/ms-teams/ms-teams.constants';
import {
  specialAuthorityEformsSupportEmail,
  specialAuthorityUrl,
} from '@app/features/access/pages/sa-eforms/sa-eforms.constants';
import { uciSupportEmail } from '@app/features/access/pages/uci/uci.constants';

import { AppEnvironment, EnvironmentName } from './environment.model';

/**
 * @description
 * Production environment populated with the default
 * environment information and appropriate overrides.
 *
 * NOTE: This environment is for local development from
 * within a container, and not used within the deployment
 * pipeline. For pipeline config mapping see main.ts and
 * the AppConfigModule.
 */
export const environment: AppEnvironment = {
  production: true,
  apiEndpoint: 'http://localhost:5050',
  configEndpoint: 'http://localhost:8080',
  authEndpoint: 'https://dev.common-sso.justice.gov.bc.ca',
  authRealm: 'BCPS',
  caseManagement: {
    showAUFLink: true,
    showCaseImportLink: false
  },
  environmentName: EnvironmentName.LOCAL,
  applicationUrl: 'http://localhost:4200',
  demsImportURL: 'https://p.zpa-auth.net/IevTunx4Bg/doauth?origurl=https%3A%2F%2Fauf%2Etest%2Ejustice%2Egov%2Ebc%2Eca%2FEdt%2Easpx%23&domain=test.agencies.justice.gov.bc.ca',
  // demsImportURL: 'https://dems.dev.jag.gov.bc.ca/Edt.aspx#/import/',
  emails: {
    providerIdentitySupport: 'jpsprovideridentityportal@gov.bc.ca',
    specialAuthorityEformsSupport: specialAuthorityEformsSupportEmail,
    hcimAccountTransferSupport: hcimWebAccountTransferSupport,
    hcimEnrolmentSupport: hcimWebEnrolmentSupport,
    driverFitnessSupport: driverFitnessSupportEmail,
    digitalEvidenceSupport: digitalEvidenceSupportEmail,
    uciSupport: uciSupportEmail,
    msTeamsSupport: msTeamsSupportEmail,
    doctorsTechnologyOfficeSupport: doctorsTechnologyOfficeEmail,
  },
  urls: {
    bcscSupport: `https://www2.gov.bc.ca/gov/content/governments/government-id/bcservicescardapp/help`,
    bcscMobileSetup: 'https://id.gov.bc.ca/account',
    specialAuthority: specialAuthorityUrl,
    doctorsTechnologyOffice: doctorsTechnologyOfficeUrl,
  },
  keycloakConfig: {
    config: {
      url: 'https://dev.common-sso.justice.gov.bc.ca/auth',
      realm: 'BCPS',
      clientId: 'PIDP-WEBAPP',
    },
    initOptions: {
      onLoad: 'check-sso',
      // LJW See if this removes warning for samesite
      silentCheckSsoFallback: true,
    },
  },
};
