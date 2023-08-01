import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { IsActiveMatchOptions } from '@angular/router';

import { Observable, map, of } from 'rxjs';

import {
  DashboardHeaderConfig,
  DashboardMenuItem,
  DashboardRouteMenuItem,
  IDashboard,
} from '@bcgov/shared/ui';
import { ArrayUtils } from '@bcgov/shared/utils';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { PartyService } from '@app/core/party/party.service';
import { AccessTokenService } from '@app/features/auth/services/access-token.service';
import { AuthService } from '@app/features/auth/services/auth.service';
import { OrganizationDetailsResource } from '@app/features/organization-info/pages/organization-details/organization-details-resource.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';
import { PortalRoutes } from '@app/features/portal/portal.routes';
import { PermissionsService } from '@app/modules/permissions/permissions.service';
import { Role } from '@app/shared/enums/roles.enum';

@Component({
  selector: 'app-portal-dashboard',
  templateUrl: './portal-dashboard.component.html',
  styleUrls: ['./portal-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalDashboardComponent implements IDashboard {
  public logoutRedirectUrl: string;
  public username: Observable<string>;
  public email: Observable<string>;
  public organization?: Observable<string>;

  public headerConfig: DashboardHeaderConfig;
  public brandConfig: { imgSrc: string; imgAlt: string };
  public showMenuItemIcons: boolean;
  public responsiveMenuItems: boolean;
  public menuItems: DashboardMenuItem[];

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private authService: AuthService,
    private partyService: PartyService,
    private portalResource: PortalResource,

    private resource: OrganizationDetailsResource,
    private permissionsService: PermissionsService,
    accessTokenService: AccessTokenService
  ) {
    this.logoutRedirectUrl = `${this.config.applicationUrl}/${this.config.routes.auth}`;
    this.username = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.name ?? ''));

    this.email = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.preferred_username ?? ''));

    this.headerConfig = { theme: 'dark', allowMobileToggle: true };
    this.brandConfig = {
      imgSrc: '/assets/images/diam-logo-small.svg',
      imgAlt: 'DIAM Portal Logo',
    };
    this.showMenuItemIcons = true;
    this.responsiveMenuItems = false;
    this.menuItems = this.createMenuItems();
    this.organization = this.getOrganization();
  }

  public getOrganization(): Observable<any> {
    const response = this.portalResource
      .getProfileStatus(this.partyService.partyId)
      .pipe(
        map((result: ProfileStatus | null) => {
          return result?.status.organizationDetails.orgName; // return back same result.
        })
      );

    return response;
  }

  public onLogout(): void {
    this.authService.logout(this.logoutRedirectUrl);
  }

  private createMenuItems(): DashboardMenuItem[] {
    const linkActiveOptions = {
      matrixParams: 'exact',
      queryParams: 'exact',
      paths: 'exact',
      fragment: 'exact',
    } as IsActiveMatchOptions;
    return [
      new DashboardRouteMenuItem(
        'Profile',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'profile' },
          linkActiveOptions,
        },
        'assignment_ind'
      ),
      ...ArrayUtils.insertResultIf<DashboardRouteMenuItem>(
        this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [
          new DashboardRouteMenuItem(
            'Organization Info',
            {
              commands: PortalRoutes.MODULE_PATH,
              extras: { fragment: 'organization' },
              linkActiveOptions,
            },
            'corporate_fare'
          ),
        ]
      ),
      new DashboardRouteMenuItem(
        'Applications Access',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'access' },
          linkActiveOptions,
        },
        'assignment'
      ),
      ...ArrayUtils.insertResultIf<DashboardRouteMenuItem>(
        this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [
          new DashboardRouteMenuItem(
            'Training',
            {
              commands: PortalRoutes.MODULE_PATH,
              extras: { fragment: 'training' },
              linkActiveOptions,
            },
            'school'
          ),
        ]
      ),
      ...ArrayUtils.insertResultIf<DashboardRouteMenuItem>(
        this.permissionsService.hasRole([Role.ADMIN]),
        () => [
          new DashboardRouteMenuItem(
            'Administration Panel',
            {
              commands: PortalRoutes.MODULE_PATH,
              extras: { fragment: 'admin' },
              linkActiveOptions,
            },
            'admin_panel_settings'
          ),
        ]
      ),
      new DashboardRouteMenuItem(
        'History',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'history' },
          linkActiveOptions,
        },
        'restore'
      ),
      new DashboardRouteMenuItem(
        'Get Support',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'support' },
          linkActiveOptions,
        },
        'help_outline'
      ),
    ];
  }
}
