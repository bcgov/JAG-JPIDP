import { EnvironmentConfig } from './environment-config.model';

export type environmentName = EnvironmentName;

export enum EnvironmentName {
  PRODUCTION = 'prod',
  TEST = 'test',
  DEVELOP = 'dev',
  LOCAL = 'local',
}

export interface AppEnvironment extends EnvironmentConfig {
  // Only indicates that Angular has been built
  // using --configuration=production
  production: boolean;
  caseManagement: {
    showAUFLink: boolean;
    showCaseImportLink: boolean;
    showCaseToolsLink: boolean;
    stickyAgencyCodes: string[];
  };
  emails: {
    providerIdentitySupport: string;
    specialAuthorityEformsSupport: string;
    hcimAccountTransferSupport: string;
    hcimEnrolmentSupport: string;
    driverFitnessSupport: string;
    digitalEvidenceSupport: string;
    uciSupport: string;
    msTeamsSupport: string;
    doctorsTechnologyOfficeSupport: string;
  };
  launch: {
    bcLawDiscPortalLabel: string;
    publicDisclosurePortalLabel: string;
    subAgencyAufPortalLabel: string;
    bcpsDemsPortalLabel: string;
    jamPorPortalLabel: string;
    outOfCustodyPortalLabel: string;
    policeToolsCaseAccessLabel: string;
  };
  urls: {
    bcscSupport: string;
    jamPorPortalUrl: string;
    bcscMobileSetup: string;
    specialAuthority: string;
    doctorsTechnologyOffice: string;
    bcLawDiscPortalUrl: string;
    subAgencyAufPortalUrl: string;
    bcpsDemsPortalUrl: string;
    publicDiscPortalUrl: string;
  };
}
