import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { BehaviorSubject, Observable } from 'rxjs';

import { PermissionsService } from '@app/modules/permissions/permissions.service';

import { AlertCode } from './enums/alert-code.enum';
import { StatusCode } from './enums/status-code.enum';
import { ProfileStatusAlert } from './models/profile-status-alert.model';
import { ProfileStatus } from './models/profile-status.model';
import { accessSectionKeys } from './state/access/access-group.model';
import { PortalSectionStatusKey } from './state/portal-section-status-key.type';
import { PortalState, PortalStateBuilder } from './state/portal-state.builder';
import { profileSectionKeys } from './state/profile/profile-group.model';

@Injectable({
  providedIn: 'root',
})
export class PortalService {
  /**
   * @description
   * Copy of the provided profile status from the most recent
   * update of the service.
   */
  private _profileStatus: ProfileStatus | null;
  /**
   * @description
   * State for driving the displayed groups and sections of
   * the portal.
   */
  private _state$: BehaviorSubject<PortalState>;
  /**
   * @description
   * List of HTTP response controlled alert messages for display
   * in the portal.
   */
  private _alerts: ProfileStatusAlert[];
  /**
   * @description
   * Whether all profile information has been completed, and
   * no access requests have been made.
   */
  private _completedProfile: boolean;

  public constructor(
    private router: Router,
    private permissionsService: PermissionsService
  ) {
    this._profileStatus = null;
    this._state$ = new BehaviorSubject<PortalState>(null);
    this._alerts = [];
    this._completedProfile = false;
  }

  public get state$(): Observable<PortalState> {
    return this._state$.asObservable();
  }

  public get alerts(): ProfileStatusAlert[] {
    return this._alerts;
  }

  public get completedProfile(): boolean {
    return this._completedProfile;
  }

  public get hiddenSections(): PortalSectionStatusKey[] {
    const status = this._profileStatus?.status;

    if (!status) {
      return [];
    }

    return (Object.keys(status) as PortalSectionStatusKey[]).filter(
      (key: PortalSectionStatusKey) =>
        status[key].statusCode === StatusCode.HIDDEN
    );
  }

  public updateState(profileStatus: ProfileStatus | null): void {
    if (!profileStatus) {
      this.clearState();
      return;
    }

    this._profileStatus = profileStatus;
    this._alerts = this.getAlerts(profileStatus);
    this._completedProfile = this.hasCompletedProfile(profileStatus);
    const builder = new PortalStateBuilder(
      this.router,
      this.permissionsService
    );
    this._state$.next(builder.createState(profileStatus));
  }

  private getAlerts(profileStatus: ProfileStatus): ProfileStatusAlert[] {
    return profileStatus.alerts.map((alert) => {
      switch (alert) {
        case AlertCode.PLR_BAD_STANDING:
          return {
            heading: 'There is a problem with your college licence',
            content: 'Contact your college for more information.',
          };
        case AlertCode.TRANSIENT_ERROR:
          return {
            heading: 'Having trouble verifying your college licence?',
            content: `Your licence may not be active yet. Try again in 24 hours. If this problem persists, contact your college.`,
          };
        case AlertCode.JUM_VALIDATION_ERROR:
          return {
            heading: 'Having trouble verifying your JUSTIN Username?',
            content:
              'The JUSTIN Username you provided may not be correct. Confirm your JUSTIN Username and try again. If this problem persists, contact DEMS user support at BCPS.Disclosure.Support@gov.bc.ca for assistance.',
          };
        case AlertCode.PENDING_REQUEST:
          return {
            heading: 'Pending access request!',
            content: 'Your access request is pending for approval',
          };
        case AlertCode.LAWYER_STATUS_ERROR:
          return {
            heading: 'Lawyer credentials invalid!',
            content:
              'Your BC Law account does not appear to be valid, please check that your BC Services Card information matches your BC Law Credentials and check your membership is active',
          };
        case AlertCode.PERSON_VERIFICATION_ERROR:
          return {
            heading: 'Personal information validation error!',
            content: 'Your personal information does not appear to be correct',
          };
        case AlertCode.VERIFIED_CREDENTIAL_MISMATCH: {
          return {
            heading: 'Verifiable credential validation error!',
            content:
              'Your provided credentials cannot be verified completely - you may request access to applications but they will be held, pending approval',
          };
        }
      }
    });
  }

  private hasCompletedProfile(profileStatus: ProfileStatus | null): boolean {
    if (!profileStatus) {
      return false;
    }

    const status = profileStatus.status;

    // Assumes key absence is a requirement and should be
    // purposefully skipped in the profile completed check
    return (
      profileSectionKeys.every((key) =>
        status[key] ? status[key].statusCode === StatusCode.COMPLETED : true
      ) &&
      accessSectionKeys.every((key) =>
        status[key] ? status[key].statusCode !== StatusCode.COMPLETED : true
      )
    );
  }

  private clearState(): void {
    this._profileStatus = null;
    this._state$.next(null);
    this._alerts = [];
    this._completedProfile = false;
  }
}
