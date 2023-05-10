export interface PartyModel {
  username?: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
  email?: string;
  enabled: boolean;
  identityProvider?: string;
  created?: Date;
  participantId: number;
  keycloakUserId?: string;
  userIssues: Record<string, string>;
  systemsAccess: Record<string, SystemUserModel>;
}

export interface SystemUserModel {
  system: string;
  username: string;
  enabled: boolean;
  accountType: string;
  key: string;
  isAdmin: boolean;
  roles: string[];
  regions: string[];
}
