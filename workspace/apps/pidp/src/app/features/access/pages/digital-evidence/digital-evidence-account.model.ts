export interface DemsAccount {
  organizationType: string;
  organizationName: string;
  participantId: string;
  assignedRegions: AssignedRegion[];
  keyData?: string;
}
export interface AssignedRegion {
  regionId: number;
  regionName: string;
  assignedAgency: string;
}

export interface UserValidationResponse {
  partyId: number;
  key: string;
  message: string;
  validated: boolean;
  tooManyAttempts: boolean;
}
