import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AlertType } from '@bcgov/shared/ui';

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
    this.description = `If you act as Duty Counsel then you may manage access to your Duty Counsel cases here.`;
  }

  public get hint(): string {
    return '';
  }

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
        this.getStatusCode() === StatusCode.READY ||
        this.getStatusCode() === StatusCode.AVAILABLE ||
        this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.DIGITAL_EVIDENCE_COUNSEL),
      disabled: !(demographicsComplete && orgComplete),
    };
  }

  public get statusType(): AlertType {
    return this.getStatusCode() === StatusCode.READY ? 'info' : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();

    return statusCode === StatusCode.AVAILABLE
      ? 'For existing users of DEMS only'
      : statusCode === StatusCode.COMPLETED || statusCode === StatusCode.READY
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
