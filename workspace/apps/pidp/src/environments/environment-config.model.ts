import { KeycloakOptions } from 'keycloak-angular';

import { environmentName } from './environment.model';

export interface EnvironmentConfig {
  apiEndpoint: string;
  authEndpoint: string;
  configEndpoint: string;
  authRealm: string;
  environmentName: environmentName;
  demsImportURL: string;
  applicationUrl: string;
  keycloakConfig: KeycloakOptions;
}
