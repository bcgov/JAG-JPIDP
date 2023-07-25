import { KeycloakProfile } from 'keycloak-js';

export interface BrokerProfile extends KeycloakProfile {
  attributes: {
    birthdate: string;
    gender: string;
    member_status: string;
    member_status_code: string;
  };
}
