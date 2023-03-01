export enum CaseStatus {
  NewRequest = 'REQUESTED',
  RemoveRequested = 'REMOVE',
  Active = 'ACTIVE',
  Pending = 'PENDING',
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
