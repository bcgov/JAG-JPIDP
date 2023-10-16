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
