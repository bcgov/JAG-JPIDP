import type { EnvironmentConfig } from "./environment-config.model";

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
  urls: {
    approvalUrl: string;
    diamUrl: string;
  };
}
