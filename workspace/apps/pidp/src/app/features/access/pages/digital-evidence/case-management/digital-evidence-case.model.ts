export enum CaseStatus {
  NewRequest = 'In Progress',
  RemoveRequested = 'Remove Requested',
  Active = 'Active',
  Pending = 'Pending',
  Completed = 'Complete',
}

export interface Field {
  id: number;
  name: string;
  value: any;
  display: boolean;
}

export interface DigitalEvidenceCase {
  name: string;
  description: string;
  key: string;
  justinStatus: JustinCaseStatus;
  agencyFileNumber: string;
  details: string;
  status: string;
  fields: Field[] | [];
  id: number;
}

export interface JustinCaseStatus {
  value: string;
  demsCandidate: boolean;
  description: string;
}

export interface DigitalEvidenceCaseAccessRequest {
  partyId: number;
  caseId: number;
  key: string;
  toolsCaseRequest: boolean;
  agencyFileNumber: string;
  name: string;
}

export interface DigitalEvidenceCaseRequest extends DigitalEvidenceCase {
  requestedOn: Date;
  requestId: number;
  assignedOn: Date | null;
  removalRequest: boolean;
  requestStatus: CaseStatus;
}
