export interface SubmittingAgency {
  name: string;
  idpHint: string;
  code: string;
  levelOfAssurance: number;
  clientCertExpiry: Date;
  hasRealm: boolean;
  hasIdentityProvider: boolean;
  hasIdentityProviderLink: boolean;
  warnings: string[];
}
