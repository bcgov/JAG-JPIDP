import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { AfterViewChecked, Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, Observable, catchError, map, noop, of, tap } from 'rxjs';

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
import { AssignedRegion } from './digital-evidence-account.model';
import { DigitalEvidenceFormState } from './digital-evidence-form-state';
import { DigitalEvidenceResource } from './digital-evidence-resource.service';
import {
  digitalEvidenceSupportEmail,
  digitalEvidenceUrl,
} from './digital-evidence.constants';

@Component({
  selector: 'app-digital-evidence',
  templateUrl: './digital-evidence.page.html',
  styleUrls: ['./digital-evidence.page.scss'],
})
export class DigitalEvidencePage
  extends AbstractFormPage<DigitalEvidenceFormState>
  implements OnInit {
  public formState: DigitalEvidenceFormState;
  public title: string;

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
  public userIsPublic?: boolean;
  public folioId?: number;
  public accessRequestFailed: boolean;
  public digitalEvidenceSupportEmail: string;
  public formControlNames: string[];
  public selectedOption = 0;
  public displayedColumns: string[] = ['regionName', 'assignedAgency'];
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
    fb: FormBuilder
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
    this.dataSource = new MatTableDataSource();
    this.identityProvider$ = this.authorizedUserService.identityProvider$;
    this.result = '';
    this.defenceValidationMessage = '';
    this.policeAgency = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.identity_provider ?? ''));

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
          this.formState.DefenceUniqueId.setValidators([]);
          this.formState.DefenceUniqueId.clearValidators();

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
  protected performSubmission(): Observable<void> {
    const partyId = this.partyService.partyId;

    if (this.selectedOption == 1) {
      return partyId && this.formState.json
        ? this.resource.requestAccess(
          partyId,
          this.formState.OrganizationType.value,
          this.formState.OrganizationName.value,
          this.formState.ParticipantId.value,
          this.formState.AssignedRegions?.value || [],
          this.formState.OOCUniqueIdValid?.value || null,

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
        this.formState.OOCUniqueId?.value || null,

      )
      : EMPTY;
  }
  public showFormControl(formControlName: string): boolean {
    return this.formControlNames.includes(formControlName);
  }

  public userInAgency(): boolean {
    return this.organizationType.isSubmittingAgency;
  }

  public checkUniqueID(val: string): void {
    console.log("Val %o", val);
  }

  public onChange(data: number): void {
    this.selectedOption = data;
  }

  public onRequestAccess(): void {
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
            this.formState.AssignedRegions?.value || [],
            this.formState.OOCUniqueId?.value || null,
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
            this.formState.AssignedRegions?.value || [],
            this.formState.OOCUniqueId?.value || null,

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
