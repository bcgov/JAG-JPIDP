import { Inject } from '@angular/core';
import { Router } from '@angular/router';

import { ArrayUtils } from '@bcgov/shared/utils';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';
import { PermissionsService } from '@app/modules/permissions/permissions.service';
import { Group } from '@app/shared/enums/groups.enum';
import { Role } from '@app/shared/enums/roles.enum';

import { StatusCode } from '../enums/status-code.enum';
import { ProfileStatus } from '../models/profile-status.model';
import { DigitalEvidenceCaseManagementPortalSection } from './access/digital-evidence-case-management-section.class';
import { DigitalEvidenceCounselPortalSection } from './access/digital-evidence-counsel-portal-section.class';
import { DigitalEvidencePortalSection } from './access/digital-evidence-portal-section.class';
import { DriverFitnessPortalSection } from './access/driver-fitness-portal-section.class';
import { HcimAccountTransferPortalSection } from './access/hcim-account-transfer-portal-section.class';
import { HcimEnrolmentPortalSection } from './access/hcim-enrolment-portal-section.class';
import { JamPorPortalSection } from './access/jam-por-portal-section.class';
import { SaEformsPortalSection } from './access/sa-eforms-portal-section.class';
import { SitePrivacySecurityPortalSection } from './access/site-privacy-security-checklist-portal-section.class';
import { AdministratorPortalSection } from './admin/admin-panel-portal-section.class';
import { TransactionsPortalSection } from './history/transactions-portal-section.class';
import { AdministratorInfoPortalSection } from './organization/administrator-information-portal-section';
import { FacilityDetailsPortalSection } from './organization/facility-details-portal-section.class';
import { OrganizationDetailsPortalSection } from './organization/organization-details-portal-section.class';
import { PortalSectionStatusKey } from './portal-section-status-key.type';
import { IPortalSection } from './portal-section.model';
import { CollegeCertificationPortalSection } from './profile/college-certification-portal-section.class';
import { DemographicsPortalSection } from './profile/demographics-portal-section.class';
import { UserAccessAgreementPortalSection } from './profile/user-access-agreement-portal-section.class';
import { ComplianceTrainingPortalSection } from './training/compliance-training-portal-section.class';

/**
 * @description
 * Group keys as a readonly tuple to allow iteration
 * at runtime.
 */
export const portalStateGroupKeys = [
  'profile',
  'access',
  'organization',
  'training',
  'admin',
  'history',
] as const;

/**
 * @description
 * Union of keys generated from the tuple.
 */
export type PortalStateGroupKey = (typeof portalStateGroupKeys)[number];

export type PortalState = Record<PortalStateGroupKey, IPortalSection[]> | null;

export class PortalStateBuilder {
  public constructor(
    private router: Router,
    private permissionsService: PermissionsService
  ) {}

  public createState(
    profileStatus: ProfileStatus
  ): Record<PortalStateGroupKey, IPortalSection[]> {
    // TODO move registration into parent module
    return {
      profile: this.createProfileGroup(profileStatus),
      access: this.createAccessGroup(profileStatus),
      organization: this.createOrganizationGroup(profileStatus),
      training: this.createTrainingGroup(profileStatus),
      admin: this.createAdminGroup(profileStatus),
      history: this.createHistoryGroup(),
    };
  }

  private createAdminGroup(profileStatus: ProfileStatus): IPortalSection[] {
    return [
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasRole([Role.ADMIN]),
        () => [new AdministratorPortalSection(profileStatus, this.router)]
      ),
    ];
  }

  // TODO see where the next few enrolments lead and then drop these methods
  //      for building out the portal state using factories, but premature
  //      optimization until more is known

  // TODO have these be registered from the modules to a service to
  //      reduce the spread of maintenance and updates. For example,
  //      centralize feature flagging into their own modules have
  //      those modules register those artifacts to services

  private createProfileGroup(profileStatus: ProfileStatus): IPortalSection[] {
    return [
      new DemographicsPortalSection(profileStatus, this.router),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.insertSection('collegeCertification', profileStatus) &&
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [
          new CollegeCertificationPortalSection(profileStatus, this.router),
        ]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when API exists and ready for production, or
        // TODO replace || with && to keep it flagged when API exists
        this.insertSection('userAccessAgreement', profileStatus) ||
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new UserAccessAgreementPortalSection(profileStatus, this.router)]
      ),
    ];
  }

  private createOrganizationGroup(
    profileStatus: ProfileStatus
  ): IPortalSection[] {
    return [
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when API exists and ready for production, or
        // TODO replace || with && to keep it flagged when API exists
        this.insertSection('organizationDetails', profileStatus),
        () => [new OrganizationDetailsPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when API exists and ready for production, or
        // TODO replace || with && to keep it flagged when API exists
        this.insertSection('facilityDetails', profileStatus),
        () => [new FacilityDetailsPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when API exists and ready for production, or
        // TODO replace || with && to keep it flagged when API exists
        this.permissionsService.hasRole([Role.ADMIN]) &&
          this.insertSection('administratorInfo', profileStatus),
        () => [new AdministratorInfoPortalSection(profileStatus, this.router)]
      ),
      // TODO - temporarily removed the endorsements section
      //...ArrayUtils.insertResultIf<IPortalSection>(
      //  // TODO remove permissions when API exists and ready for production, or
      //  // TODO replace || with && to keep it flagged when API exists
      //  //this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]) ||
      //  this.permissionsService.hasRole([Role.USER]) ||
      //    this.insertSection('endorsements', profileStatus),
      //  () => [new EndorsementsPortalSection(profileStatus, this.router)]
      //),
    ];
  }

  private createAccessGroup(profileStatus: ProfileStatus): IPortalSection[] {
    return [
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.insertSection('saEforms', profileStatus) &&
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new SaEformsPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.insertSection('hcimAccountTransfer', profileStatus) &&
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new HcimAccountTransferPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when ready for production
        this.insertSection('hcimEnrolment', profileStatus) &&
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new HcimEnrolmentPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when API exists and ready for production, or
        // TODO replace || with && to keep it flagged when API exists
        this.insertSection('sitePrivacySecurityChecklist', profileStatus) ||
          this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new SitePrivacySecurityPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        // TODO remove permissions when ready for production
        this.insertSection('driverFitness', profileStatus),
        () => [new DriverFitnessPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasGroup([Group.BSPS]) ||
          this.insertSection('digitalEvidence', profileStatus),
        () => [new DigitalEvidencePortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasRole([Role.JAM_POR]) ||
          this.insertSection('jamPor', profileStatus),
        () => [new JamPorPortalSection(profileStatus, this.router)]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasGroup([Group.BSPS]) ||
          this.insertSection('digitalEvidenceCaseManagement', profileStatus),
        () => [
          new DigitalEvidenceCaseManagementPortalSection(
            profileStatus,
            this.router
          ),
        ]
      ),
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasGroup([Group.BSPS]) ||
          this.insertSection('digitalEvidenceCounsel', profileStatus),
        () => [
          new DigitalEvidenceCounselPortalSection(profileStatus, this.router),
        ]
      ),
    ];
  }

  private createTrainingGroup(profileStatus: ProfileStatus): IPortalSection[] {
    return [
      ...ArrayUtils.insertResultIf<IPortalSection>(
        this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [new ComplianceTrainingPortalSection(profileStatus, this.router)]
      ),
    ];
  }

  private createHistoryGroup(): IPortalSection[] {
    return [new TransactionsPortalSection(this.router)];
  }

  private insertSection(
    portalSectionKey: PortalSectionStatusKey,
    profileStatus: ProfileStatus
  ): boolean {
    const statusCode = profileStatus.status[portalSectionKey]?.statusCode;
    return statusCode && statusCode !== StatusCode.HIDDEN;
  }
}
