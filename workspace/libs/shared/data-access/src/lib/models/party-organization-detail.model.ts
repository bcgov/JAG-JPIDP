import { CorrectionServiceCode, OrganizationCode } from '../enums';

export interface PartyOrganizationDetails {
  id: number;
  orgName: string;
  organizationCode: OrganizationCode;
  correctionService: string;
  submittingAgency: any;
  lawSociety: boolean;
  justiceSectorCode: number;
  correctionServiceCode: CorrectionServiceCode;
  justiceSectorService: string;
  employeeIdentifier: string;
}
