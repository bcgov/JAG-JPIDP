export class OrganizationUserType {
  public organizationType: string;
  public organizationName: string;
  public participantId: string;
  public isSubmittingAgency: boolean;

  constructor() {
    this.organizationType = '';
    this.organizationName = '';
    this.participantId = '';
    this.isSubmittingAgency = false;
  }
}
