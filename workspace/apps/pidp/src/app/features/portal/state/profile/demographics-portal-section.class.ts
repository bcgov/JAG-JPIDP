import { Router } from '@angular/router';

import { Observable } from 'rxjs';

import { AlertType } from '@bcgov/shared/ui';

import { ProfileRoutes } from '@app/features/profile/profile.routes';
import { ShellRoutes } from '@app/features/shell/shell.routes';

import { BasePortalSection } from '../../base-portal-section';
import { StatusCode } from '../../enums/status-code.enum';
import { ProfileStatus } from '../../models/profile-status.model';
import { PortalSectionAction } from '../portal-section-action.model';
import { PortalSectionKey } from '../portal-section-key.type';
import { PortalSectionProperty } from '../portal-section-property.model';
import { IPortalSection } from '../portal-section.model';
import { DemographicsSection } from './demographic-section.model';

export class DemographicsPortalSection
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
    this.key = 'demographics';
    this.heading = 'Personal Information';
    this.description = 'Provide personal information in order to proceed.';
    this.order = this.GetOrder(profileStatus.status.demographics);
  }

  public get hint(): string {
    return [
      StatusCode.ERROR,
      StatusCode.COMPLETED,
      StatusCode.LOCKEDCOMPLETE,
    ].includes(this.getStatusCode())
      ? ''
      : '1 min to complete';
  }

  public get properties(): PortalSectionProperty[] {
    const { firstName, lastName, email, phone } = this.getSectionStatus();
    return this.getStatusCode() === StatusCode.COMPLETED ||
      this.getStatusCode() === StatusCode.LOCKEDCOMPLETE
      ? [
          {
            key: 'fullName',
            value: `${firstName} ${lastName}`,
          },
          {
            key: 'email',
            value: email,
          },
          {
            key: 'phone',
            value: phone,
          },
        ]
      : [];
  }

  /**
   * @description
   * Get the properties that define the action on the section.
   */
  public get action(): PortalSectionAction {
    const statusCode = this.getStatusCode();
    return {
      label: statusCode === StatusCode.LOCKEDCOMPLETE ? '' : 'Update',
      route: ProfileRoutes.routePath(ProfileRoutes.PERSONAL_INFO),
      disabled:
        statusCode === StatusCode.ERROR || statusCode === StatusCode.LOCKED,
    };
  }

  public get statusType(): AlertType {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.ERROR
      ? 'danger'
      : statusCode === StatusCode.COMPLETED ||
        statusCode === StatusCode.LOCKEDCOMPLETE
      ? 'success'
      : 'warn';
  }

  public get status(): string {
    const statusCode = this.getStatusCode();
    return statusCode === StatusCode.ERROR
      ? ''
      : statusCode === StatusCode.COMPLETED ||
        statusCode === StatusCode.LOCKEDCOMPLETE
      ? 'Completed'
      : 'Incomplete';
  }

  public performAction(): void | Observable<void> {
    this.router.navigate([ShellRoutes.routePath(this.action.route)]);
  }

  private getSectionStatus(): DemographicsSection {
    return this.profileStatus.status.demographics;
  }

  private getOrder(): number {
    return this.profileStatus.status.demographics.order;
  }

  private getStatusCode(): StatusCode {
    return this.getSectionStatus().statusCode;
  }
}
