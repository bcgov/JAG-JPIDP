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
  requestedDate: Date;
  assignedDate: Date | null;
  requestStatus: CaseStatus;
}
