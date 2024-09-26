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

export class JamPorPortalSection
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
    this.key = 'jamPor';
    this.heading = 'JUSTIN Protection Order Portal';
    this.description = this.getDescription();
    this.order = this.GetOrder(this.profileStatus.status.jamPor);
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

    const demographicsComplete =
      demographicsStatusCode === StatusCode.COMPLETED ||
      demographicsStatusCode === StatusCode.LOCKEDCOMPLETE ||
      demographicsStatusCode === StatusCode.HIDDENCOMPLETE;

    return {
      label:
        this.getStatusCode() === StatusCode.COMPLETED
          ? 'View'
          : this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : this.getStatusCode() === StatusCode.APPROVED
          ? 'Pending'
          : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.JAM_POR),
      disabled: !(
        demographicsComplete &&
        this.getStatusCode() !== StatusCode.REQUIRESAPPROVAL &&
        this.getStatusCode() !== StatusCode.APPROVED &&
        this.getStatusCode() !== StatusCode.MISSINGREQUIREDCLAIMS &&
        this.getStatusCode() !== StatusCode.ERROR &&
        this.getStatusCode() !== StatusCode.DENIED
      ),
    };
  }

  public get launch(): PortalSectionLaunchAction {
    const config = AppInjector.get(APP_CONFIG);

    const label = config.launch.jamPorPortalLabel;

    const url = config.urls.jamPorPortalUrl;

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
      ? 'Your request has been denied - please contact JUSTIM JAM support for more information on why the request was denied.'
      : this.getStatusCode() === StatusCode.PENDING
      ? 'Your request is pending and should complete shortly'
      : this.getStatusCode() === StatusCode.MISSINGREQUIREDCLAIMS
      ? 'Your account needs to be setup in JUSTIN in order to proceed'
      : this.getStatusCode() === StatusCode.ERROR
      ? 'Your request resulted in an error - please contact JUSTIM JAM Support at the email below'
      : 'Request access to JAM POP';
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
      : this.getStatusCode() === StatusCode.MISSINGREQUIREDCLAIMS
      ? 'missing-claims'
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
      : statusCode === StatusCode.MISSINGREQUIREDCLAIMS
      ? 'Your account does not have the correct access'
      : statusCode === StatusCode.ERROR
      ? 'Request Failed'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.jamPor.statusCode;
  }

  private getOrgName(): string {
    return this.profileStatus.status.organizationDetails.orgName;
  }
}
