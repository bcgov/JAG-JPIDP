import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AlertType } from '@bcgov/shared/ui';

import { OrganizationInfoRoutes } from '@app/features/organization-info/organization-info.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { BasePortalSection } from '../../base-portal-section';
import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
import { PortalSectionProperty } from '../portal-section-property.model';
import { IPortalSection } from '../portal-section.model';
import { PartyOrganizationDetailsSection } from './organization-details-section.model';

export class OrganizationDetailsPortalSection
  extends BasePortalSection
  implements IPortalSection
{
  public readonly key: PortalSectionKey;
  public heading: string;
  public description: string;
  public order: number;

  public constructor(
    private profileStatus: ProfileStatus,
    private router: Router
  ) {
    super();
    this.key = 'organizationDetails';
    this.heading = 'Organization Details';
    this.description = this.getDescription();
    this.order = this.GetOrder(profileStatus.status.organizationDetails);
  }

  public get hint(): string {
    return [StatusCode.ERROR, StatusCode.COMPLETED].includes(
      this.getStatusCode()
    )
      ? ''
      : '';
  }

  /**
   * @description
   * Get the properties that define the action on the section.
   */
  public get properties(): PortalSectionProperty[] {
    const statusCode = this.getStatusCode();
    const {
      employeeIdentifier,
      orgName,
      correctionService,
      justiceSectorService,
    } = this.getSectionStatus();
    const response = [StatusCode.ERROR, StatusCode.COMPLETED].includes(
      statusCode
    )
      ? [
          {
            key: 'orgName',
            value: orgName,
            label: 'Organization: ',
          },
          {
            key: 'employeeIdentifier',
            value: employeeIdentifier,
            label: 'Identity Verification:',
          },

          {
            key: 'CorrectionService',
            value: correctionService,
          },
          {
            key: 'JusticSectorService',
            value: justiceSectorService,
          },
        ]
      : [];

    if (
      !this.profileStatus.status.organizationDetails?.submittingAgency &&
      !this.profileStatus.status.organizationDetails.lawSociety
    ) {
      response.push({
        key: 'status',
        value:
          statusCode !== StatusCode.ERROR &&
          this.profileStatus.status.organizationDetails?.statusCode ===
            StatusCode.COMPLETED
            ? 'Verified'
            : 'Not Verified - Update to provide your JUSTIN user info in order to request access to systems',
        label: 'JUSTIN User Status:',
      });
    } else if (this.profileStatus.status.organizationDetails.lawSociety) {
      response.push({
        key: 'organizationName',
        value: this.profileStatus.status.organizationDetails?.orgName,
        label: 'Organization:',
      });
    } else {
      response.push({
        key: 'organizationName',
        value:
          this.profileStatus.status.organizationDetails?.submittingAgency.name,
        label: 'Organization:',
      });
      response.push({
        key: 'agencyCode',
        value:
          this.profileStatus.status.organizationDetails?.submittingAgency.code,
        label: 'Agency Code:',
      });
    }
    return response;
  }
  public get action(): PortalSectionAction {
    return {
      label:
        this.profileStatus.status.organizationDetails?.statusCode ===
          StatusCode.LOCKEDCOMPLETE ||
        this.profileStatus.status.organizationDetails?.statusCode ===
          StatusCode.COMPLETED
          ? ''
          : 'Update',
      route: OrganizationInfoRoutes.routePath(
        OrganizationInfoRoutes.ORGANIZATION_DETAILS
      ),
      disabled:
        this.profileStatus.status.organizationDetails?.statusCode ===
        StatusCode.LOCKEDCOMPLETE,
    };
  }

  public getDescription(): string {
    return this.profileStatus.status.organizationDetails?.statusCode ===
      StatusCode.LOCKEDCOMPLETE ||
      this.profileStatus.status.organizationDetails?.statusCode ===
        StatusCode.COMPLETED
      ? 'Your oragnization information is completed and validated'
      : 'Please provide details about your organization in order to proceed to the next steps';
  }

  public get statusType(): AlertType {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.ERROR
      ? 'danger'
      : statusCode === StatusCode.LOCKEDCOMPLETE
      ? 'completed'
      : statusCode === StatusCode.COMPLETED
      ? 'completed'
      : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.COMPLETED ||
      statusCode === StatusCode.LOCKEDCOMPLETE
      ? 'Completed'
      : statusCode === StatusCode.ERROR
      ? 'Invalid JUSTIN Details Entered'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getSectionStatus(): PartyOrganizationDetailsSection {
    return this.profileStatus.status.organizationDetails;
  }

  private getStatusCode(): StatusCode {
    // TODO remove null check once API exists
    return this.profileStatus.status.organizationDetails?.statusCode;
  }
}
