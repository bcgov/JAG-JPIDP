export enum CourtRequestStatus {
  NewRequest = 'In Progress',
  RemoveRequested = 'Remove Requested',
  Submitted = 'Submitted',
  SubmittedFuture = 'Future',
  Active = 'Active',
  Pending = 'Pending',
  Completed = 'Completed',
}

export interface CourtLocation {
  city: string;
  name: string;
  code: string;
}

export interface CourtLocationRequest {
  partyId: number;
  requestedOn?: Date;
  requestId?: number;
  assignedOn?: Date;
  validFrom: Date;
  courtLocation: CourtLocation;
  validUntil: Date;
  removalRequest?: boolean;
  requestStatus: CourtRequestStatus;
}
