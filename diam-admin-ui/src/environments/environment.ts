import { EnvironmentName } from "./environment.model";
import { environment as defaultEnvironment } from './environment.prod';

export const environment = {
    ...defaultEnvironment,
  production: false,
  environmentName: EnvironmentName.DEVELOP,
};