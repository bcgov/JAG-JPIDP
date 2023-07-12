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

export class DigitalEvidenceCaseManagementPortalSection
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
    this.key = 'digitalEvidenceCaseManagement';
    this.heading = 'Digital Evidence Case Access';
    this.description = `Manage access to your Digital Evidence Cases here.`;
    this.order = this.GetOrder(
      this.profileStatus.status.digitalEvidenceCaseManagement
    );
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
          ? 'Manage'
          : this.getStatusCode() === StatusCode.PENDING
          ? 'View'
          : 'Request',
      route: AccessRoutes.routePath(
        AccessRoutes.DIGITAL_EVIDENCE_CASE_MANAGEMENT
      ),
      disabled: !(
        digitalEvidenceStatusCode === StatusCode.COMPLETED &&
        (demographicsStatusCode === StatusCode.COMPLETED ||
          demographicsStatusCode === StatusCode.LOCKEDCOMPLETE) &&
        (organizationStatusCode === StatusCode.COMPLETED ||
          organizationStatusCode === StatusCode.LOCKEDCOMPLETE)
      ),
    };
  }

  public get hint(): string {
    return '';
  }

  public get status(): string {
    const digitalEvidenceStatusCode =
      this.profileStatus.status.digitalEvidence.statusCode;
    return digitalEvidenceStatusCode === StatusCode.COMPLETED
      ? 'Available'
      : 'Pending DEMS Enrollment';
  }

  public get statusType(): AlertType {
    const digitalEvidenceStatusCode =
      this.profileStatus.status.digitalEvidence.statusCode;
    return digitalEvidenceStatusCode === StatusCode.COMPLETED
      ? 'completed'
      : 'greyed';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidenceCaseManagement.statusCode;
  }
}
