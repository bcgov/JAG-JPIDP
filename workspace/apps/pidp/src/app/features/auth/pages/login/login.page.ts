import { Component, Inject, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, Observable, catchError, exhaustMap, map, of, startWith, tap } from 'rxjs';

import {
  DashboardHeaderConfig,
  DialogOptions,
  HtmlComponent,
} from '@bcgov/shared/ui';
import { ConfirmDialogComponent } from '@bcgov/shared/ui';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { DocumentService } from '@app/core/services/document.service';
import { LookupService } from '@app/modules/lookup/lookup.service';
import { LoginOptionLookup, Lookup } from '@app/modules/lookup/lookup.types';

import { IdentityProvider } from '../../enums/identity-provider.enum';
import { AuthService } from '../../services/auth.service';
import { AuthConfigService } from '../../services/auth-config-service';
import { LoginConfigModel } from '../../models/loginConfigModel';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '@app/core/services/toast.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
})
export class LoginPage implements OnInit {
  public title: string;
  public headerConfig: DashboardHeaderConfig;
  public loginCancelled: boolean;
  public organizations: Lookup[];
  public filteredLoginOptions!: Observable<LoginOptionLookup[]>;
  public submittingAgencies: LoginOptionLookup[];
  public agency: LoginOptionLookup | undefined;

  public bcscSupportUrl: string;
  public bcscMobileSetupUrl: string;
  public specialAuthorityUrl: string;
  public providerIdentitySupportEmail: string;
  public idpHint: IdentityProvider;
  public governmentAgency: LoginOptionLookup = {
    code: 0,
    idpHint: IdentityProvider.AZUREIDIR,
    name: 'Government User',
  };
  public IdentityProvider = IdentityProvider;
  public loginOptions: Observable<LoginConfigModel[]>;
  public noLoginOptions: boolean;
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  selectedLoginName: FormControl = new FormControl();

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private authService: AuthService,
    private authConfigService: AuthConfigService,
    private route: ActivatedRoute,
    private router: Router,
    private toastService: ToastService,
    private dialog: MatDialog,
    private lookupService: LookupService,
    private documentService: DocumentService
  ) {
    const routeSnapshot = this.route.snapshot;

    this.title = routeSnapshot.data.title;
    this.headerConfig = { theme: 'dark', allowMobileToggle: false };
    this.loginCancelled = routeSnapshot.queryParams.action === 'cancelled';
    this.bcscSupportUrl = this.config.urls.bcscSupport;
    this.organizations = this.lookupService.organizations;
    this.bcscMobileSetupUrl = this.config.urls.bcscMobileSetup;
    this.loginOptions = this.getLoginOptions();
    this.noLoginOptions = false;
    this.specialAuthorityUrl = this.config.urls.specialAuthority;
    this.providerIdentitySupportEmail =
      this.config.emails.providerIdentitySupport;
    this.idpHint = routeSnapshot.data.idpHint;
    this.submittingAgencies = this.lookupService.submittingAgencies.filter(
      (agency) => agency.idpHint?.length > 0
    );
    this.submittingAgencies.push(this.governmentAgency);


  }

  public onScrollToAnchor(): void {
    this.router.navigate([], {
      fragment: 'systems',
      queryParamsHandling: 'preserve',
    });
  }

  public onAgencyLogin(): void {
    this.onLogin(IdentityProvider.SUBMITTING_AGENCY, this.agency?.idpHint);
  }

  public getLoginOptions(): Observable<LoginConfigModel[]> {

    return this.authConfigService.getLoginOptions().pipe(map((res: LoginConfigModel[]) => {
      if (res.length === 0) {
        this.noLoginOptions = true;
      }

      return res;
    }), catchError(err => {

      this.toastService.openErrorToast("Unable to load configuration - if this persists please contact support");
      console.error("Error getting config %o", err);
      this.noLoginOptions = true;
      return of([]);
    })
    );
  }

  public getOptions(config: LoginConfigModel): Observable<LoginOptionLookup[]> {
    return this.filteredLoginOptions;
  }

  public ngOnInit(): void {

    this.filteredLoginOptions = this.selectedLoginName.valueChanges.pipe(
      startWith(''),
      map((value) => this.filterAgencies(value || ''))
    );
  }

  public onOptionLogin(option: LoginConfigModel): void {
    const letIdpString = option.idp;
    const typedIdpString = letIdpString as keyof typeof IdentityProvider;
    const idp: IdentityProvider = IdentityProvider[typedIdpString];
    this.onLogin(idp, option.idp);
  }

  public onLogin(idpHint?: IdentityProvider, idpStringVal?: string): void {
    if (this.idpHint === IdentityProvider.AZUREIDIR) {
      this.login(this.idpHint);
      return;
    }

    const data: DialogOptions = {
      title: 'Collection Notice',
      component: HtmlComponent,
      data: {
        content: this.documentService.getPIdPCollectionNotice(),
      },
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .pipe(
        exhaustMap((result) =>
          result
            ? idpStringVal != null
              ? this.loginDyanmicIdp(idpStringVal)
              : this.login(idpHint ?? this.idpHint)
            : EMPTY
        )
      )
      .subscribe();
  }

  private loginDyanmicIdp(idpHint: string): Observable<void> {
    const endorsementToken =
      this.route.snapshot.queryParamMap.get('endorsement-token');
    return this.authService.login({
      idpHint: idpHint,
      redirectUri:
        this.config.applicationUrl +
        (endorsementToken ? `?endorsement-token=${endorsementToken}` : ''),
    });
  }

  private filterAgencies(value: string): LoginOptionLookup[] {
    if (!value || value.length < 2) {
      return [];
    }
    if (this.agency && value !== this.agency.name) {
      this.agency = undefined;
    }

    const filterValue = value.toLowerCase();

    const response = this.submittingAgencies.filter((option) =>
      option.name.toLowerCase().includes(filterValue)
    );
    return response.length > 0 ? response : [];
  }

  public agencySelected(): boolean {
    return this.agency !== undefined;
  }

  public onSelectionChanged(event: MatAutocompleteSelectedEvent): void {

    if (event.option.value === this.governmentAgency.name) {
      // add government agency
      this.agency = this.governmentAgency;
    } else {
      this.agency = this.lookupService.submittingAgencies.filter(
        (agency) => agency.name === event.option.value
      )[0];
    }
  }

  private login(idpHint: IdentityProvider): Observable<void> {
    const endorsementToken =
      this.route.snapshot.queryParamMap.get('endorsement-token');
    return this.authService.login({
      idpHint: idpHint,
      redirectUri:
        this.config.applicationUrl +
        (endorsementToken ? `?endorsement-token=${endorsementToken}` : ''),
    });
  }
}
