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

export class DigitalEvidencePortalSection implements IPortalSection {
  public readonly key: PortalSectionKey;
  public heading: string;
  public description: string;

  public constructor(
    private profileStatus: ProfileStatus,
    private router: Router
  ) {
    this.key = 'digitalEvidence';
    this.heading = 'Digital Evidence Management System';
    this.description = `Enrol here for access to Digital Evidence Management System application.`;
  }

  public get hint(): string {
    return '1 min to complete';
  }
  /**
   * @description
   * Get the properties that define the action on the section.
   */
  public get action(): PortalSectionAction {
    const demographicsStatusCode =
      this.profileStatus.status.demographics.statusCode;
    return {
      label: this.getStatusCode() === StatusCode.COMPLETED ? 'View' : 'Request',
      route: AccessRoutes.routePath(AccessRoutes.DIGITAL_EVIDENCE),
      disabled: demographicsStatusCode !== StatusCode.COMPLETED,
    };
  }

  public get statusType(): AlertType {
    return this.getStatusCode() === StatusCode.COMPLETED ? 'success' : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.AVAILABLE
      ? 'For existing users of DEMS only'
      : statusCode === StatusCode.COMPLETED
      ? 'Completed'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getStatusCode(): StatusCode {
    return this.profileStatus.status.digitalEvidence.statusCode;
  }
}
