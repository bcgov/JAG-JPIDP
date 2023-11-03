import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, catchError, of, tap } from 'rxjs';

import {
  CorrectionServiceCode,
  JusticeSectorCode,
  NoContent,
  OrganizationCode,
} from '@bcgov/shared/data-access';

import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { FormUtilsService } from '@app/core/services/form-utils.service';
import { LoggerService } from '@app/core/services/logger.service';
import { IdentityProvider } from '@app/features/auth/enums/identity-provider.enum';
import { SubmittingAgencyResolver } from '@app/features/auth/models/submitting-agency-resolver';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';
import { LookupService } from '@app/modules/lookup/lookup.service';
import { LoginOptionLookup, Lookup } from '@app/modules/lookup/lookup.types';

import { OrganizationDetailsFormState } from './organization-details-form-state';
import { OrganizationDetailsResource } from './organization-details-resource.service';
import { OrganizationDetails } from './organization-details.model';

@Component({
  selector: 'app-organization-details',
  templateUrl: './organization-details.page.html',
  styleUrls: ['./organization-details.page.scss'],
})
export class OrganizationDetailsPage
  extends AbstractFormPage<OrganizationDetailsFormState>
  implements OnInit {
  public title: string;
  public formState: OrganizationDetailsFormState;
  public organizations: (Lookup & { disabled: boolean })[];
  public healthAuthorities: Lookup[];
  public lawEnforcements: Lookup[];
  public justiceSectors: Lookup[];
  public submittingAgencies: LoginOptionLookup[];
  public isPrePopulatedOrg: boolean;
  public correctionServices: Lookup[];
  public lawSocieties: Lookup[];
  public IdentityProvider = IdentityProvider;
  public selectedOption = 0;

  public constructor(
    protected dialog: MatDialog,
    protected formUtilsService: FormUtilsService,
    private route: ActivatedRoute,
    private router: Router,
    private partyService: PartyService,
    private resource: OrganizationDetailsResource,
    private logger: LoggerService,
    private authorizedUserService: AuthorizedUserService,
    private lookupService: LookupService,
    fb: FormBuilder
  ) {
    super(dialog, formUtilsService);

    const routeData = this.route.snapshot.data;
    this.title = routeData.title;
    this.isPrePopulatedOrg = false;
    this.formState = new OrganizationDetailsFormState(fb);
    this.submittingAgencies = this.lookupService.submittingAgencies.filter(
      (agency) => agency.idpHint?.length > 0
    );
    this.healthAuthorities = this.lookupService.healthAuthorities;
    this.justiceSectors = this.lookupService.justiceSectors;
    this.lawEnforcements = this.lookupService.lawEnforcements;
    this.correctionServices = this.lookupService.correctionServices;
    this.lawSocieties = this.lookupService.lawSocieties;
    this.authorizedUserService.identityProvider$.subscribe((val) => {
      if (val === IdentityProvider.BCPS) {
        this.organizations = this.lookupService.organizations
          .filter(
            (org: Lookup<OrganizationCode>) =>
              org.code === OrganizationCode.JusticeSector
          )
          .map((org: Lookup<OrganizationCode>) => ({
            ...org,
            disabled: false,
          }));
      } else if (val === IdentityProvider.BCSC) {
        this.organizations = this.lookupService.organizations.map(
          (organization) => ({
            ...organization,
            disabled: !(
              organization.code === OrganizationCode.correctionService ||
              organization.code === OrganizationCode.LawSociety
            ),
          })
        );
      } else if (val === IdentityProvider.SUBMITTING_AGENCY) {
        this.organizations = this.lookupService.organizations.map(
          (organization) => ({
            ...organization,
            disabled: !(
              organization.code === OrganizationCode.correctionService ||
              organization.code === OrganizationCode.LawSociety
            ),
          })
        );
      }
    });

    this.organizations = this.lookupService.organizations.map(
      (organization) => ({
        ...organization,
        disabled: organization.code === null,
      })
    );
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public isPrePopulated(): boolean {
    return this.isPrePopulatedOrg;
  }

  public onChange(data: number): void {
    this.selectedOption = data;

    // justice
    if (this.selectedOption == 1) {
      // only one option
      if (this.justiceSectors?.length === 1) {
        this.formState.justiceSectorCode.setValue(this.justiceSectors[0].code);
      }
    }
    if (this.selectedOption == 4) {
      this.formState.correctionServiceCode.setValidators([Validators.required]);
    }
  }

  public ngOnInit(): void {
    this.formState.justiceSectorCode.clearValidators();
    this.formState.justiceSectorCode.reset();
    this.formState.healthAuthorityCode.clearValidators();
    this.formState.healthAuthorityCode.reset();
    this.formState.lawEnforcementCode.clearValidators();
    this.formState.lawEnforcementCode.reset();
    this.formState.correctionServiceCode.clearValidators();
    this.formState.correctionServiceCode.reset();

    const partyId = this.partyService.partyId;
    if (!partyId) {
      this.logger.error('No party ID was provided');
      return this.navigateToRoot();
    }

    this.resource
      .get(partyId)
      .pipe(
        tap((model: OrganizationDetails | null) => {
          if (model) {
            if (this.organizations.length === 1) {
              model.organizationCode = this.organizations[0].code;

              this.formState.patchValue(model);

              if (
                model.organizationCode === 1 &&
                this.justiceSectors.length === 1
              ) {
                this.formState.justiceSectorCode.patchValue(
                  this.justiceSectors[0].code
                );
              }
            }
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === HttpStatusCode.NotFound) {
            this.navigateToRoot();
          }
          return of(null);
        })
      )
      .subscribe();
  }

  protected performSubmission(): NoContent {
    const partyId = this.partyService.partyId;

    return partyId && this.formState.json
      ? this.resource.update(partyId, this.formState.json)
      : EMPTY;
  }

  protected afterSubmitIsSuccessful(): void {
    this.navigateToRoot();
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}
