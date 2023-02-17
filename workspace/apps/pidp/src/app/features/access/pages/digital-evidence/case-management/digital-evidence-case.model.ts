export enum CaseStatus {
  NewRequest = 'REQUESTED',
  RemoveRequested = 'REMOVE',
  Active = 'ACTIVE',
  Pending = 'PENDING',
}

export interface DigitalEvidenceCase {
  requestedDate: Date;
  assignedDate: Date;
  name: string;
  status: CaseStatus;
  caseNumber: string;

}
