import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AlertType } from '@bcgov/shared/ui';

import { APP_CONFIG } from '@app/app.config';
import { AppInjector } from '@app/app.module';
import { AccessRoutes } from '@app/features/access/access.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { BasePortalSection } from '../../base-portal-section';
import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
import { PortalSectionLaunchAction } from '../portal-section-launch-action.model';
import { IPortalSection } from '../portal-section.model';

export class DigitalEvidencePortalSection
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
    this.key = 'digitalEvidence';
    this.heading = 'Digital Evidence and Disclosure Management System (DEMS)';
    this.description = this.getDescription();
    this.order = this.GetOrder(this.profileStatus.status.digitalEvidence);
  }

  public get hint(): string {
    return '';
  }
  /**
   * @description
   * Get the properties that define the action on the section.
   */
  public get action(): PortalSectionAction {
    const demographicsStatusCode =
      this.profileStatus.status.demographics.statusCode;
    const organizationStatusCode =
      this.profileStatus.status.organizationDetails.statusCode;
    const demographicsComplete =
      demographicsStatusCode === StatusCode.COMPLETED ||
      demographicsStatusCode === StatusCode.LOCKEDCOMPLETE ||
      demographicsStatusCode === StatusCode.HIDDENCOMPLETE;
    const orgComplete =
      organizationStatusCode === StatusCode.COMPLETED ||
      organizationStatusCode === StatusCode.LOCKEDCOMPLETE ||
      organizationStatusCode === StatusCode.HIDDENCOMPLETE;
    return {
      label:
        this.getStatusCode() === StatusCode.COMPLETED
          ? 'View'
          : this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : this.getStatusCode() === StatusCode.APPROVED
          ? 'Pending'
          : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.DIGITAL_EVIDENCE),
      disabled: !(
        demographicsComplete &&
        orgComplete &&
        this.getStatusCode() !== StatusCode.REQUIRESAPPROVAL &&
        this.getStatusCode() !== StatusCode.APPROVED &&
        this.getStatusCode() !== StatusCode.ERROR &&
        this.getStatusCode() !== StatusCode.DENIED
      ),
    };
  }

  public get launch(): PortalSectionLaunchAction {
    const config = AppInjector.get(APP_CONFIG);

    const label = this.isOOCA(this.profileStatus)
      ? config.launch.publicDisclosurePortalLabel
      : this.getOrgName() === 'Justice Sector'
      ? config.launch.bcpsDemsPortalLabel
      : this.getOrgName() === 'BC Law Society'
      ? config.launch.bcLawDiscPortalLabel
      : this.isSubAgency()
      ? config.launch.subAgencyAufPortalLabel
      : undefined;
    const url =
      this.getOrgName() === 'BC Law Society'
        ? config.urls.bcLawDiscPortalUrl
        : this.getOrgName() === 'Justice Sector'
        ? config.urls.bcpsDemsPortalUrl
        : config.urls.subAgencyAufPortalUrl;
    return {
      hidden: false,
      label: label,
      newWindow: true,
      url: url,
      disabled: this.getStatusCode() !== StatusCode.COMPLETED,
    };
  }

  public getDescription(): string {
    return this.getStatusCode() === StatusCode.COMPLETED
      ? 'Your enrolment is complete. You can view the terms of enrolment by clicking the View button'
      : this.getStatusCode() === StatusCode.REQUIRESAPPROVAL
      ? 'Your request is being reviewed - you will be emailed once a decision is made'
      : this.getStatusCode() === StatusCode.APPROVED
      ? 'Your request has been approved - your account should be available shortly'
      : this.getStatusCode() === StatusCode.DENIED
      ? 'Your request has been denied - please contact DEMS support for more information on why the request was denied.'
      : this.getStatusCode() === StatusCode.PENDING
      ? 'Your request is pending and should complete shortly'
      : this.getStatusCode() === StatusCode.ERROR
      ? 'Your request resulted in an error - please contact BCPS Support at the email below'
      : this.isOOCA(this.profileStatus)
      ? 'Request access to your Disclosure material'
      : 'Request access to enroll in DEMS.';
  }

  public isOOCA(profileStatus: ProfileStatus): boolean {
    return (
      profileStatus.status.demographics.userTypes &&
      profileStatus.status.demographics.userTypes.includes(
        'OutOfCustodyAccused'
      )
    );
  }

  public get statusType(): AlertType {
    return this.getStatusCode() === StatusCode.COMPLETED
      ? 'completed'
      : this.getStatusCode() === StatusCode.AVAILABLE ||
        this.getStatusCode() === StatusCode.INCOMPLETE
      ? 'available'
      : this.getStatusCode() === StatusCode.PENDING
      ? 'pending'
      : this.getStatusCode() === StatusCode.REQUIRESAPPROVAL
      ? 'pending-approval'
      : this.getStatusCode() === StatusCode.APPROVED
      ? 'greyed'
      : this.getStatusCode() === StatusCode.DENIED
      ? 'danger'
      : this.getStatusCode() === StatusCode.ERROR
      ? 'danger'
      : 'greyed';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();
    const demographicsStatusCode =
      this.profileStatus.status.demographics.statusCode;
    if (
      demographicsStatusCode === StatusCode.INCOMPLETE ||
      demographicsStatusCode === StatusCode.AVAILABLE
    ) {
      return 'Complete prior step(s)';
    }
    return statusCode === StatusCode.AVAILABLE ||
      this.getStatusCode() === StatusCode.INCOMPLETE
      ? 'Access Request Available'
      : statusCode === StatusCode.COMPLETED
      ? 'Completed'
      : statusCode === StatusCode.PENDING
      ? 'Pending'
      : statusCode === StatusCode.REQUIRESAPPROVAL
      ? 'Pending Approval'
      : statusCode === StatusCode.APPROVED
      ? 'Approved - awaiting completion'
      : statusCode === StatusCode.DENIED
      ? 'Request reviewed and denied'
      : statusCode === StatusCode.ERROR
      ? 'Request Failed'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidence.statusCode;
  }

  private getOrgName(): string {
    return this.profileStatus.status.organizationDetails.orgName;
  }

  private isSubAgency(): boolean {
    return this.profileStatus.status.organizationDetails.submittingAgency;
  }
}
