import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { OrganizationCode } from '@bcgov/shared/data-access';
import { AlertType } from '@bcgov/shared/ui';

import { OrganizationInfoRoutes } from '@app/features/organization-info/organization-info.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
import { PortalSectionProperty } from '../portal-section-property.model';
import { IPortalSection } from '../portal-section.model';
import { PartyOrganizationDetailsSection } from './organization-details-section.model';

export class OrganizationDetailsPortalSection implements IPortalSection {
  public readonly key: PortalSectionKey;
  public heading: string;
  public description: string;

  public constructor(
    private profileStatus: ProfileStatus,
    private router: Router
  ) {
    this.key = 'organizationDetails';
    this.heading = 'Organization Details';
    this.description = 'Provide details about your organization.';
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
            : 'Not Verified',
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
        StatusCode.LOCKED_COMPLETE
          ? ''
          : 'Update',
      route: OrganizationInfoRoutes.routePath(
        OrganizationInfoRoutes.ORGANIZATION_DETAILS
      ),
      disabled:
        this.profileStatus.status.organizationDetails?.statusCode ===
        StatusCode.LOCKED_COMPLETE,
    };
  }

  public get statusType(): AlertType {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.ERROR
      ? 'danger'
      : statusCode === StatusCode.LOCKED_COMPLETE
      ? 'success'
      : statusCode === StatusCode.COMPLETED
      ? 'success'
      : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.COMPLETED ||
      statusCode === StatusCode.LOCKED_COMPLETE
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
