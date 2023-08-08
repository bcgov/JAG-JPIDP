import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
} from '@angular/core';
import { IsActiveMatchOptions } from '@angular/router';

import { Observable, first, map, take, tap } from 'rxjs';

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
import { StatusCode } from '@app/features/portal/enums/status-code.enum';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';
import { PortalRoutes } from '@app/features/portal/portal.routes';
import { LookupService } from '@app/modules/lookup/lookup.service';
import { PermissionsService } from '@app/modules/permissions/permissions.service';
import { Role } from '@app/shared/enums/roles.enum';

@Component({
  selector: 'app-portal-dashboard',
  templateUrl: './portal-dashboard.component.html',
  styleUrls: ['./portal-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalDashboardComponent implements IDashboard, OnInit {
  public logoutRedirectUrl: string;
  public username: Observable<string>;
  public email: Observable<string>;
  public organization?: Observable<string>;
  public loading: boolean;
  public headerConfig: DashboardHeaderConfig;
  public brandConfig: { imgSrc: string; imgAlt: string };
  public showMenuItemIcons: boolean;
  public responsiveMenuItems: boolean;
  public menuItems!: DashboardMenuItem[];
  public displayMenus: boolean;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private authService: AuthService,
    private partyService: PartyService,
    private portalResource: PortalResource,
    private lookupService: LookupService,
    private resource: OrganizationDetailsResource,
    private permissionsService: PermissionsService,
    accessTokenService: AccessTokenService
  ) {
    this.logoutRedirectUrl = `${this.config.applicationUrl}/${this.config.routes.auth}`;
    this.username = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.name ?? ''));
    this.displayMenus = false;
    this.loading = true;
    this.email = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.email ?? ''));

    this.headerConfig = { theme: 'dark', allowMobileToggle: true };
    this.brandConfig = {
      imgSrc: '/assets/images/diam-logo-small.svg',
      imgAlt: 'DIAM Portal Logo',
    };
    this.showMenuItemIcons = true;
    this.responsiveMenuItems = false;
  }

  public getOrganization(): Observable<string> {
    const response = this.portalResource
      .getProfileStatus(this.partyService.partyId)
      .pipe(
        tap((result: ProfileStatus | null) => {
          if (
            result?.status.organizationDetails.statusCode ===
            StatusCode.HIDDENCOMPLETE
          ) {
            const filtered = this.menuItems.filter(
              (item) => item.label !== 'Organization Info'
            );
            this.menuItems = filtered;
          }
          if (
            result?.status.demographics.statusCode === StatusCode.HIDDENCOMPLETE
          ) {
            const filtered = this.menuItems.filter(
              (item) => item.label !== 'Profile'
            );
            this.menuItems = filtered;
          }
        }),
        map((result: ProfileStatus | null) => {
          let response = result?.status.organizationDetails.orgName;
          const sectors = this.lookupService.justiceSectors;

          if (result?.status.organizationDetails.organizationCode === 1) {
            result?.status.organizationDetails.justiceSectorCode;
            const sector = sectors.find(
              (sec) =>
                sec.code ===
                result?.status.organizationDetails.justiceSectorCode
            );
            response += ' ' + sector?.name;
          }
          return response || ''; // return back same result.
        })
      );

    return response || '';
  }

  public ngOnInit(): void {
    this.menuItems = this.createMenuItems();

    this.organization = this.getOrganization();
    this.displayMenus = true;
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

    let items = [
      ...ArrayUtils.insertResultIf<DashboardRouteMenuItem>(
        this.permissionsService.hasRole([Role.FEATURE_PIDP_DEMO]),
        () => [
          new DashboardRouteMenuItem(
            'Profile',
            {
              commands: PortalRoutes.MODULE_PATH,
              extras: { fragment: 'profile' },
              linkActiveOptions,
            },

            'assignment_ind'
          ),
        ]
      ),
      new DashboardRouteMenuItem(
        'Profile',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'profile' },
          linkActiveOptions,
        },

        'assignment_ind'
      ),

      new DashboardRouteMenuItem(
        'Organization Info',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'organization' },
          linkActiveOptions,
        },
        'corporate_fare'
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
      // new DashboardRouteMenuItem(
      //   'History',
      //   {
      //     commands: PortalRoutes.MODULE_PATH,
      //     extras: { fragment: 'history' },
      //     linkActiveOptions,
      //   },
      //   'restore'
      // ),
      new DashboardRouteMenuItem(
        'Support',
        {
          commands: PortalRoutes.MODULE_PATH,
          extras: { fragment: 'support' },
          linkActiveOptions,
        },
        'help_outline'
      ),
    ];

    this.portalResource
      .getProfileStatus(this.partyService.partyId)
      .pipe(
        tap((result: ProfileStatus | null) => {
          if (
            result?.status.organizationDetails.statusCode ===
            StatusCode.HIDDENCOMPLETE
          ) {
            const filtered = items.filter(
              (item) => item.label !== 'Organization Info'
            );
            this.menuItems = filtered;
          }
          if (
            result?.status.demographics.statusCode === StatusCode.HIDDENCOMPLETE
          ) {
            const filtered = items.filter((item) => item.label !== 'Profile');
            items = filtered;
          }
        })
      )
      .subscribe(() => (this.loading = false));

    return items;
  }
}
