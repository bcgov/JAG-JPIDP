export enum CaseStatus {
  NewRequest = 'In Progress',
  RemoveRequested = 'Remove Requested',
  Active = 'Active',
  Pending = 'Pending',
  Completed = 'Completed',
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
  agencyFileNumber: string;
  status: string;
  fields: Field[] | [];
  id: number;
}

export interface DigitalEvidenceCaseRequest extends DigitalEvidenceCase {
  requestedOn: Date;
  requestId: number;
  assignedOn: Date | null;
  removalRequest: boolean;
  requestStatus: CaseStatus;
}
