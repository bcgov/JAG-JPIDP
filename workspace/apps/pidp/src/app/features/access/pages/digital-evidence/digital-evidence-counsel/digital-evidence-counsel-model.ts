export enum CourtRequestStatus {
  NewRequest = 'In Progress',
  RemoveRequested = 'Remove Requested',
  Submitted = 'Submitted',
  SubmittedFuture = 'Future',
  Active = 'Active',
  Pending = 'Pending',
  Complete = 'Complete',
}

export interface CourtLocation {
  active: boolean;
  staffed: boolean;
  edtId: number;
  status: string;
  details: string;
  name: string;
  code: string;
  key: string;
  edtFields: EdtField[];
}

export interface EdtField {
  id: number;
  name: string;
  value: any;
  display: boolean;
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
