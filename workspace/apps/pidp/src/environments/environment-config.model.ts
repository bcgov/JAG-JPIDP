import { KeycloakOptions } from 'keycloak-angular';

import { UrlOptions, environmentName } from './environment.model';

export interface EnvironmentConfig {
  apiEndpoint: string;
  authEndpoint: string;
  authRealm: string;
  environmentName: environmentName;
  applicationUrl: string;
  keycloakConfig: KeycloakOptions;
  urls: UrlOptions;
}
