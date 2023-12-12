import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, Inject, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, Observable, catchError, interval, map, noop, of, takeWhile, tap } from 'rxjs';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { DocumentService } from '@app/core/services/document.service';
import { LoggerService } from '@app/core/services/logger.service';
import { IdentityProvider } from '@app/features/auth/enums/identity-provider.enum';
import { AccessTokenService } from '@app/features/auth/services/access-token.service';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';
import { StatusCode } from '@app/features/portal/enums/status-code.enum';

import { FormUtilsService } from '@core/services/form-utils.service';

import { PartyUserTypeResource } from '../../../../features/admin/shared/usertype-resource.service';
import { OrganizationUserType } from '../../../../features/admin/shared/usertype-service.model';
import { BcpsAuthResourceService } from './auth/bcps-auth-resource.service';
import { DigitalEvidenceCase } from './case-management/digital-evidence-case.model';
import { AssignedRegion, PublicDisclosureAccess, UserValidationResponse } from './digital-evidence-account.model';
import { DigitalEvidenceFormState } from './digital-evidence-form-state';
import { DigitalEvidenceResource } from './digital-evidence-resource.service';
import {
  digitalEvidenceSupportEmail,
  digitalEvidenceUrl,
} from './digital-evidence.constants';

@Component({
  selector: 'app-digital-evidence',
  templateUrl: './digital-evidence.page.html',
  styleUrls: ['./digital-evidence.page.scss']
})
export class DigitalEvidencePage
  extends AbstractFormPage<DigitalEvidenceFormState>
  implements OnInit {
  public formState: DigitalEvidenceFormState;
  public title: string;
  public panelOpenState = false;
  public organizationType: OrganizationUserType;
  public assignedRegions: AssignedRegion[] = [];
  public digitalEvidenceUrl: string;
  public dataSource: MatTableDataSource<AssignedRegion>;
  public defenceValidationMessage: string;
  public identityProvider$: Observable<IdentityProvider>;
  //public userType$: Observable<OrganizationUserType[]>;
  public IdentityProvider = IdentityProvider;
  public collectionNotice: string;
  public completed: boolean | null;
  public pending: boolean | null;
  public policeAgency: Observable<string>;
  public result: string;
  public userIsBCPS?: boolean;
  public userIsLawyer?: boolean;
  public showDataMismatchError: boolean;
  public userIsPublic?: boolean;
  public folioId?: number;
  public validatingUser: boolean;
  public userCodeStatus: string;
  public userValidationMessage?: string;
  public accessRequestFailed: boolean;
  public OOCCodeInvalid: boolean;
  public refreshCount: number;
  public publicAccessDenied: boolean;
  public outOfCustodyCodeAlreadyRequested: boolean;
  public digitalEvidenceSupportEmail: string;
  public defenceCounselOnboardingNotice: string;
  public formControlNames: string[];
  public outOfCustodyDisclosureListing: PublicDisclosureAccess[] = [];
  public outOfCustodyDataSource: MatTableDataSource<PublicDisclosureAccess>;
  public refreshOutOfCustodyEnabled: boolean;
  public selectedOption = 0;
  public displayedColumns: string[] = ['regionName', 'assignedAgency'];
  public displayedOutOfCustodyColumns: string[] = ['requestStatus', 'keyData', 'created', 'completedOn'];
  public userTypes = [
    { id: 0, name: '--Select User Type--', disable: true },
    { id: 1, name: 'CorrectionService', disable: false },
    { id: 2, name: 'JusticeSector', disable: false },
    { id: 3, name: 'LawEnforcement', disable: false },
    { id: 4, name: 'LawSociety', disable: false },
  ];
  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private partyService: PartyService,
    private resource: DigitalEvidenceResource,
    private usertype: PartyUserTypeResource,
    private userOrgunit: BcpsAuthResourceService,
    private logger: LoggerService,
    documentService: DocumentService,
    accessTokenService: AccessTokenService,
    private authorizedUserService: AuthorizedUserService,
    fb: FormBuilder,
  ) {
    super(dialog, formUtilsService);
    const routeData = this.route.snapshot.data;
    this.title = routeData.title;
    this.digitalEvidenceUrl = digitalEvidenceUrl;
    this.organizationType = new OrganizationUserType();
    const partyId = this.partyService.partyId;
    this.userIsBCPS = false;
    this.userIsLawyer = false;
    this.userIsPublic = false;
    this.validatingUser = false;
    this.publicAccessDenied = false;
    this.refreshOutOfCustodyEnabled = false;
    this.OOCCodeInvalid = false;
    this.dataSource = new MatTableDataSource();
    this.showDataMismatchError = false;
    this.outOfCustodyDataSource = new MatTableDataSource(this.outOfCustodyDisclosureListing);
    this.identityProvider$ = this.authorizedUserService.identityProvider$;
    this.result = '';
    this.refreshCount = 0;
    this.userCodeStatus = '';
    this.outOfCustodyCodeAlreadyRequested = false;
    this.defenceValidationMessage = '';
    this.policeAgency = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.identity_provider ?? ''));
    this.defenceCounselOnboardingNotice = documentService.getDefenceCounselOnboardingNotice();
    accessTokenService.decodeToken().subscribe((n) => {
      if (n !== null) {
        this.result = n.identity_provider;
      }
    });
    this.usertype.getUserType(partyId).subscribe((data: any) => {
      this.organizationType.participantId = data['participantId'];

      if (data['organizationType']) {
        this.organizationType.organizationType = data['organizationType'];
        this.organizationType.organizationName = data['organizationName'];

        this.formState.OrganizationName.patchValue(
          this.organizationType.organizationName
        );

        this.formState.OrganizationType.patchValue(
          this.organizationType.organizationType
        );
      }
      this.organizationType.isSubmittingAgency =
        data['isSubmittingAgency'] || false;


      this.formState.ParticipantId.patchValue(
        this.organizationType.participantId
      );



      this.identityProvider$.subscribe((idp) => {
        // todo - remove IDIR
        if (idp === IdentityProvider.BCPS || idp === IdentityProvider.IDIR) {
          // if BCPS then get the crown-regions
          this.formState.OOCUniqueIdValid.setValidators([]);
          this.formState.OOCUniqueIdValid.clearValidators();

          this.userIsBCPS = true;


          this.userOrgunit
            .getUserOrgUnit(
              partyId,
              Number(this.organizationType.participantId)
            )
            .subscribe((data: any) => {
              this.assignedRegions = data;
              this.formState.AssignedRegions.patchValue(this.assignedRegions);
            });
        }
        if (idp === IdentityProvider.VERIFIED_CREDENTIALS) {
          this.userIsLawyer = true;
        }
        if (idp === IdentityProvider.BCSC) {
          this.getOutOfCustodyRequests();
          this.OOCCodeInvalid = true;
          this.userIsPublic = true;
        }
      });
    });

    this.formState = new DigitalEvidenceFormState(fb);
    this.collectionNotice =
      documentService.getDigitalEvidenceCollectionNotice();
    this.completed =
      routeData.digitalEvidenceStatusCode === StatusCode.COMPLETED;
    this.pending = routeData.digitalEvidenceStatusCode === StatusCode.PENDING;
    this.accessRequestFailed = false;
    this.digitalEvidenceSupportEmail = digitalEvidenceSupportEmail;
    this.formControlNames = [
      'OrganizationType',
      'OrganizationName',
      'AssignedRegions',
      'DefenceUniqueId',
      'ParticipantId',
      'OOCUniqueId'
    ];
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public refreshOutOfCustodyTable(): void {
    interval(4000)
      .pipe(takeWhile(() => this.refreshOutOfCustodyEnabled))
      .subscribe(() => {
        this.getOutOfCustodyRequests();
      });
  }



  protected performSubmission(): Observable<void> {
    const partyId = this.partyService.partyId;

    if (this.userIsPublic) {
      return partyId && this.formState.json
        ? this.resource.requestDisclosureAccess(
          partyId,
          this.formState.ParticipantId.value,
          this.formState.OOCUniqueIdValid?.value,
        ) : EMPTY;
    } else {

      if (this.selectedOption == 1) {
        return partyId && this.formState.json
          ? this.resource.requestAccess(
            partyId,
            this.formState.OrganizationType.value,
            this.formState.OrganizationName.value,
            this.formState.ParticipantId.value,
            this.formState.AssignedRegions?.value || [],

          )
          : EMPTY;
      }

      return partyId && this.formState.json
        ? this.resource.requestAccess(
          partyId,
          this.formState.OrganizationType.value,
          this.formState.OrganizationName.value,
          this.formState.ParticipantId.value,
          this.formState.AssignedRegions?.value || [],

        )
        : EMPTY;
    }
  }
  public showFormControl(formControlName: string): boolean {
    return this.formControlNames.includes(formControlName);
  }

  public userInAgency(): boolean {
    return this.organizationType.isSubmittingAgency;
  }

  public getOutOfCustodyRequests(): void {
    this.resource
      .getPublicCaseAccess(this.partyService.partyId)
      .pipe()
      .subscribe(
        (outOfCustodyRequests: PublicDisclosureAccess[]) => {
          this.outOfCustodyDisclosureListing = outOfCustodyRequests;
          this.outOfCustodyDataSource = new MatTableDataSource(this.outOfCustodyDisclosureListing);
          // see if all requests are complete
          const incompleteRequestResponse = outOfCustodyRequests.filter(req => req.requestStatus !== "Complete");
          if (incompleteRequestResponse.length == 0) {
            this.refreshOutOfCustodyEnabled = false;

          }

          this.refreshOutOfCustodyTable();
          if (this.refreshOutOfCustodyEnabled) {
            this.refreshCount++;
            if (this.refreshCount >= 4) {
              this.refreshOutOfCustodyEnabled = false;
              this.refreshCount = 0;
            }
          }
        });

  }

  public pad(input: string, size: number): string {

    while (input.length < size) input = "0" + input;
    return input;
  }

  public checkUniqueID(): void {

    if (this.formState.OOCUniqueId.valid) {
      const codeToCheck = this.formState.OOCUniqueId.value.replace(/\D/g, '');
      // pad code
      const codeValue = this.formState.OOCUniqueId.value;
      if (this.formState.OOCUniqueId.value.length < 6) {
        this.formState.OOCUniqueId.patchValue(this.pad(codeValue, 6))
      }
      this.validatingUser = true;
      this.OOCCodeInvalid = true;

      this.userCodeStatus = '';
      this.resource.validatePublicUniqueID(
        this.partyService.partyId,
        codeToCheck
      ).pipe(
        tap(() => {
          this.userValidationMessage = "Validating your code...";
        }),
      ).subscribe((res: UserValidationResponse | HttpErrorResponse) => {
        this.validatingUser = false;

        if (res instanceof HttpErrorResponse) {
          this.userValidationMessage = "Unable to validate at this time - please try again later";
          this.userCodeStatus = 'error';
          this.formState.OOCUniqueId.patchValue('');
        }
        else {
          if (res.tooManyAttempts) {
            this.userCodeStatus = 'too_many_attempts';
            this.formState.OOCUniqueId.patchValue('');
            this.formState.OOCUniqueId.disable();
            this.publicAccessDenied = true;

            this.userValidationMessage = 'Too many attempts - please contact BCPS for assistance';
          } else {
            if (res.alreadyActive) {
              this.formState.OOCUniqueId.patchValue('');
              this.userCodeStatus = 'valid';
              this.OOCCodeInvalid = false;
              this.formState.OOCUniqueId.markAsPristine();
              this.formState.OOCUniqueId.markAsUntouched();
              this.userValidationMessage = (res.requestStatus == "Complete") ? 'Code already used and is active - you can login to get your Disclosure package' : `Code used and in status ${res.requestStatus}`;
            }
            else if (res.dataMismatch) {
              this.userCodeStatus = 'invalid';
              this.showDataMismatchError = true;
              this.formState.OOCUniqueId.patchValue('');
              this.formState.OOCUniqueId.disable();
              this.publicAccessDenied = true;

            }
            else {
              this.userCodeStatus = (res.dataMismatch) ? 'invalid' : res.validated ? 'valid' : 'invalid';
              if (res.validated) {
                this.formState.OOCUniqueId.disable();
                this.OOCCodeInvalid = false;
              }
              else {
                this.formState.OOCUniqueId.patchValue('');
                this.formState.OOCUniqueId.markAsPristine();
                this.formState.OOCUniqueId.markAsUntouched();
                this.publicAccessDenied = false;

              }

              this.userValidationMessage = res.validated ? 'Your code is good. You can now submit your request.' : 'Check your code to make sure it\'s correct, then try again.';
            }
          }
        }
      });
    } else {
      this.userValidationMessage = undefined;
    }

  }



  public validationEntryDisabled(): boolean {
    return true;
  }

  public onChange(data: number): void {
    this.selectedOption = data;
  }

  public onRequestAccess(): void {
    if (this.userIsPublic) {
      this.resource
        .requestDisclosureAccess(
          this.partyService.partyId,
          this.formState.ParticipantId.value,
          this.formState.OOCUniqueId.value).pipe(
            tap(() => (this.pending = true)),

            catchError((error: HttpErrorResponse) => {
              if (error.status === HttpStatusCode.NotFound) {
                this.navigateToRoot();
              }
              this.accessRequestFailed = true;
              return of(noop());
            })
          )
        .subscribe({
          complete: () => {
            this.refreshOutOfCustodyEnabled = true;
            this.refreshOutOfCustodyTable();
            this.formState.OOCUniqueId.enable();
            this.formState.OOCUniqueId.patchValue('');
            this.formState.OOCUniqueId.markAsPristine();
            this.formState.OOCUniqueId.markAsUntouched();
            this.userValidationMessage = '';
            this.userCodeStatus = '';
            this.pending = false;
          },
        }
        );
    } else
      if (this.userIsLawyer) {
        this.resource
          .requestDefenceCounselAccess(
            this.partyService.partyId,
            this.formState.OrganizationType.value,
            this.formState.OrganizationName.value,
            this.formState.ParticipantId.value
          )
          .pipe(
            tap(() => (this.pending = true)),
            catchError((error: HttpErrorResponse) => {
              if (error.status === HttpStatusCode.NotFound) {
                this.navigateToRoot();
              }
              this.accessRequestFailed = true;
              return of(noop());
            })
          )
          .subscribe();
      } else {
        if (this.selectedOption == 1) {
          this.resource
            .requestAccess(
              this.partyService.partyId,
              this.formState.OrganizationType.value,
              this.formState.OrganizationName.value,
              this.formState.ParticipantId.value,
              this.formState.AssignedRegions?.value || []
            )
            .pipe(
              tap(() => (this.pending = true)),
              catchError((error: HttpErrorResponse) => {
                if (error.status === HttpStatusCode.NotFound) {
                  this.navigateToRoot();
                }
                this.accessRequestFailed = true;
                return of(noop());
              })
            )
            .subscribe();
        } else {
          this.resource
            .requestAccess(
              this.partyService.partyId,
              this.formState.OrganizationType.value,
              this.formState.OrganizationName.value,
              this.formState.ParticipantId.value,
              this.formState.AssignedRegions?.value || []
            )
            .pipe(
              tap(() => (this.pending = true)),
              catchError((error: HttpErrorResponse) => {
                if (error.status === HttpStatusCode.NotFound) {
                  this.navigateToRoot();
                }
                this.accessRequestFailed = true;
                return of(noop());
              })
            )
            .subscribe();
        }
      }
  }

  public ngOnInit(): void {
    const partyId = this.partyService.partyId;

    if (!partyId) {
      this.logger.error('No party ID was provided');
      return this.navigateToRoot();
    }

    if (this.completed === null) {
      this.logger.error('No status code was provided');
      return this.navigateToRoot();
    }

    // dynamically add form validators
    this.identityProvider$.subscribe((idp) => {
      if (idp === IdentityProvider.BCSC) {
        this.formState.OOCUniqueId.setValidators([
          Validators.required,
        ]);

      }
      if (idp === IdentityProvider.BCPS) {
        this.formState.AssignedRegions.setValidators([Validators.required]);
      }
    });
  }

  public validateDefenceId(): void {
    const partyId = this.partyService.partyId;

    this.resource
      .validateDefenceId(partyId, this.formState.DefenceUniqueId.value)
      .pipe(
        tap((response: DigitalEvidenceCase) => {
          this.formState.DefenceUniqueIdValid.patchValue(response.id);
          this.formState.DefenceUniqueId.disable();
          this.folioId = response.id;
          this.defenceValidationMessage =
            'ID Validated - you may now request access';
        }),
        catchError((error: HttpErrorResponse) => {
          this.formState.DefenceUniqueId.patchValue('');
          this.defenceValidationMessage =
            'Invalid ID entered - please correct and validate again';

          if (error.status === HttpStatusCode.NotFound) {
            this.navigateToRoot();
          }
          return of(noop());
        })
      )
      .subscribe();
  }

  public onUniqueIdInput(): void {
    if (
      this.formState.DefenceUniqueId.value &&
      this.formState.DefenceUniqueId.value.length === 3
    ) {
      this.formState.DefenceUniqueId.patchValue(
        this.formState.DefenceUniqueId.value + '-'
      );
    }
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}
