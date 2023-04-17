import { KeycloakProfile } from 'keycloak-js';

export interface BrokerProfile extends KeycloakProfile {
  attributes: {
    birthdate: string;
    gender: string;
  };
}
