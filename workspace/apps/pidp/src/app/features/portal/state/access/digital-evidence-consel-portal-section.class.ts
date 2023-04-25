import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AccessRoutes } from '@app/features/access/access.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
import { IPortalSection } from '../portal-section.model';

export class DigitalEvidenceCounselPortalSection implements IPortalSection {
  public readonly key: PortalSectionKey;
  public heading: string;
  public description: string;

  public constructor(
    private profileStatus: ProfileStatus,
    private router: Router
  ) {
    this.key = 'digitalEvidenceCounsel';
    this.heading =
      'Digital Evidence and Disclosure Management System Duty Counsel Access';
    this.description = `Manage Access to your Duty Counsel cases here.`;
  }

  public get hint(): string {
    return '2 min to complete';
  }

  public get action(): PortalSectionAction {
    const demographicsStatusCode =
      this.profileStatus.status.demographics.statusCode;
    const organizationStatusCode =
      this.profileStatus.status.organizationDetails.statusCode;
    return {
      label:
        this.getStatusCode() === StatusCode.COMPLETED
          ? 'View'
          : this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.DIGITAL_EVIDENCE_COUNSEL),
      disabled: !(
        (demographicsStatusCode === StatusCode.COMPLETED &&
          organizationStatusCode === StatusCode.COMPLETED) ||
        organizationStatusCode === StatusCode.LOCKED_COMPLETE
      ),
    };
  }

  public get status(): string {
    const statusCode = this.getStatusCode();

    return statusCode === StatusCode.AVAILABLE
      ? 'For existing users of DEMS only'
      : statusCode === StatusCode.COMPLETED
      ? 'Available'
      : statusCode === StatusCode.PENDING
      ? 'Pending'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidenceCounsel.statusCode;
  }
}
