export enum CourtRequestStatus {
  NewRequest = 'In Progress',
  RemoveRequested = 'Remove Requested',
  Active = 'Active',
  Pending = 'Pending',
  Completed = 'Completed',
}

export interface CourtLocation {
  city: string;
  name: string;
  locationId: number;
}

export interface CourtLocationAccessRequest {
  partyId: number;
  locationId: number;
  activeFrom: Date;
  activeUntil: Date;
}

export interface CourtLocationRequest extends CourtLocation {
  requestedOn: Date;
  requestId: number;
  assignedOn: Date | null;
  activeFrom: Date;
  activeUntil: Date;
  removalRequest: boolean;
  requestStatus: CourtRequestStatus;
}
