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

export class DigitalEvidenceCaseManagementPortalSection
  implements IPortalSection
{
  public readonly key: PortalSectionKey;
  public heading: string;
  public description: string;

  public constructor(
    private profileStatus: ProfileStatus,
    private router: Router
  ) {
    this.key = 'digitalEvidenceCaseManagement';
    this.heading = 'Digital Evidence and Disclosure Case Management System';
    this.description = `Manage access to your Digital Evidence Cases here.`;
  }

  public get action(): PortalSectionAction {
    const digitalEvidenceStatusCode =
      this.profileStatus.status.digitalEvidence.statusCode;
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
      route: AccessRoutes.routePath(
        AccessRoutes.DIGITAL_EVIDENCE_CASE_MANAGEMENT
      ),
      disabled: !(
        digitalEvidenceStatusCode === StatusCode.COMPLETED &&
        (demographicsStatusCode === StatusCode.COMPLETED ||
          demographicsStatusCode === StatusCode.LOCKED_COMPLETE) &&
        (organizationStatusCode === StatusCode.COMPLETED ||
          organizationStatusCode === StatusCode.LOCKED_COMPLETE)
      ),
    };
  }

  public get hint(): string {
    return '5 min to complete';
  }

  public get status(): string {
    const digitalEvidenceStatusCode =
      this.profileStatus.status.digitalEvidence.statusCode;
    return digitalEvidenceStatusCode === StatusCode.COMPLETED
      ? 'Available'
      : 'Pending DEMS Enrollment';
  }

  public get statusType(): AlertType {
    return this.getStatusCode() === StatusCode.COMPLETED ? 'info' : 'warn';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidenceCaseManagement.statusCode;
  }
}
