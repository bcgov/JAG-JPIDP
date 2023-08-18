import type { environmentName } from "./environment.model";


export interface EnvironmentConfig {
  apiEndpoint: string;
  authEndpoint: string;
  authRealm: string;
  environmentName: environmentName;
  applicationUrl: string;
}
