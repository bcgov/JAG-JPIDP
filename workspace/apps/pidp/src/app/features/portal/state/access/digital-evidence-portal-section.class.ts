import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AlertType } from '@bcgov/shared/ui';

import { AccessRoutes } from '@app/features/access/access.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { BasePortalSection } from '../../base-portal-section';
import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
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
    this.heading = 'Digital Evidence and Disclosure Management System';
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
      demographicsStatusCode === StatusCode.LOCKED_COMPLETE;
    const orgComplete =
      organizationStatusCode === StatusCode.COMPLETED ||
      organizationStatusCode === StatusCode.LOCKED_COMPLETE;
    return {
      label:
        this.getStatusCode() === StatusCode.COMPLETED
          ? 'View'
          : this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.DIGITAL_EVIDENCE),
      disabled: !(demographicsComplete && orgComplete),
    };
  }

  public getDescription(): string {
    return this.getStatusCode() === StatusCode.COMPLETED
      ? 'Your enrolment is complete. You can view the terms of enrolment by clicking the View button'
      : `Enrol here for access to Digital Evidence and Disclosure Management System application.`;
  }

  public get statusType(): AlertType {
    let u = this.getStatusCode();
    return this.getStatusCode() === StatusCode.COMPLETED
      ? 'success'
      : this.getStatusCode() === StatusCode.PENDING
      ? 'info'
      : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();

    return statusCode === StatusCode.AVAILABLE
      ? 'For existing users of DEMS only'
      : statusCode === StatusCode.COMPLETED
      ? 'Completed'
      : statusCode === StatusCode.PENDING
      ? 'Pending'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidence.statusCode;
  }
}
