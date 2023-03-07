export class OrganizationUserType {
  public organizationType: string;
  public organizationName: string;
  public participantId: string;
  public isSubmittingAgency: boolean;
  public submittingAgencyCode: string;

  constructor() {
    this.organizationType = '';
    this.organizationName = '';
    this.submittingAgencyCode = '';
    this.participantId = '';
    this.isSubmittingAgency = false;
  }
}
