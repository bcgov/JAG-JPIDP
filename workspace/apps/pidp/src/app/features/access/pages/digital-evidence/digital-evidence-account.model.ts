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
  requestStatus: string;
  alreadyActive: boolean;
  validated: boolean;
  dataMismatch: boolean;
  tooManyAttempts: boolean;
}


export interface PublicDisclosureAccess {
  completedOn: Date;
  keyData: string;
  created: Date
  requestStatus: string;
}
