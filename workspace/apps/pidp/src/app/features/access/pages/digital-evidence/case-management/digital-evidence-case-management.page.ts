import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import {
  EMPTY,
  Observable,
  catchError,
  debounceTime,
  exhaustMap,
  filter,
  map,
  noop,
  of,
  switchMap,
  tap,
} from 'rxjs';

import {
  ConfirmDialogComponent,
  DialogOptions,
  HtmlComponent,
} from '@bcgov/shared/ui';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { DocumentService } from '@app/core/services/document.service';
import { LoggerService } from '@app/core/services/logger.service';
import { ToastService } from '@app/core/services/toast.service';
import { IdentityProvider } from '@app/features/auth/enums/identity-provider.enum';
import { AccessTokenService } from '@app/features/auth/services/access-token.service';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';
import { StatusCode } from '@app/features/portal/enums/status-code.enum';

import { FormUtilsService } from '@core/services/form-utils.service';

import { PartyUserTypeResource } from '../../../../../features/admin/shared/usertype-resource.service';
import { OrganizationUserType } from '../../../../../features/admin/shared/usertype-service.model';
import { BcpsAuthResourceService } from '../auth/bcps-auth-resource.service';
import { DigitalEvidenceCaseManagementFormState } from './digital-evidence-case-management-form.state';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';
import { DigitalEvidenceCaseResource } from './digital-evidence-case-resource.service';
import {
  CaseStatus,
  DigitalEvidenceCase,
  DigitalEvidenceCaseRequest,
} from './digital-evidence-case.model';

@Component({
  selector: 'app-digital-evidence',
  templateUrl: './digital-evidence-case-management.page.html',
  styleUrls: ['./digital-evidence-case-management.page.scss'],
})
export class DigitalEvidenceCaseManagementPage
  extends AbstractFormPage<DigitalEvidenceCaseManagementFormState>
  implements OnInit
{
  public formState: DigitalEvidenceCaseManagementFormState;
  public title: string;

  public organizationType: OrganizationUserType;
  public caseListing: DigitalEvidenceCaseRequest[] = [];

  public identityProvider$: Observable<IdentityProvider>;
  //public userType$: Observable<OrganizationUserType[]>;
  public IdentityProvider = IdentityProvider;

  public collectionNotice: string;
  public completed: boolean | null;
  public pending: boolean | null;
  public policeAgency: Observable<string>;
  public result: string;
  public requestedCase!: DigitalEvidenceCase | null;
  public isCaseSearchInProgress: boolean;
  public isCaseFound: boolean;
  public accessRequestFailed: boolean;
  public requestedCaseNotFound: boolean;
  public isFindDisabled: boolean;
  public hasCaseListingResults: boolean;
  //@Input() public form!: FormGroup;
  public formControlNames: string[];
  public selectedOption = 0;
  public dataSource: MatTableDataSource<DigitalEvidenceCaseRequest>;
  public displayedColumns: string[] = [
    'agencyFileNumber',
    'requestedOn',
    'requestStatus',
    'caseAction',
  ];

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private partyService: PartyService,
    private resource: DigitalEvidenceCaseManagementResource,
    private usertype: PartyUserTypeResource,
    private userOrgunit: BcpsAuthResourceService,
    private logger: LoggerService,
    private toastService: ToastService,
    documentService: DocumentService,
    accessTokenService: AccessTokenService,
    private authorizedUserService: AuthorizedUserService,
    private digitalEvidenceCaseResource: DigitalEvidenceCaseManagementResource,
    fb: FormBuilder
  ) {
    super(dialog, formUtilsService);
    const routeData = this.route.snapshot.data;
    this.title = routeData.title;
    this.organizationType = new OrganizationUserType();
    const partyId = this.partyService.partyId;
    this.dataSource = new MatTableDataSource();
    //this.userType$ = this.usertype.getUserType(partyId);
    this.identityProvider$ = this.authorizedUserService.identityProvider$;
    this.result = '';
    this.policeAgency = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.identity_provider ?? ''));

    accessTokenService.decodeToken().subscribe((n) => {
      console.log(n.identity_provider);
      this.result = n.identity_provider;
    });
    this.usertype.getUserType(partyId).subscribe((data: any) => {
      this.organizationType.organizationType = data['organizationType'];
      this.organizationType.participantId = data['participantId'];
      this.organizationType.organizationName = data['organizationName'];
    });
    this.formState = new DigitalEvidenceCaseManagementFormState(fb);
    this.collectionNotice =
      documentService.getDigitalEvidenceCollectionNotice();
    this.completed =
      routeData.digitalEvidenceStatusCode === StatusCode.COMPLETED;
    this.pending = routeData.digitalEvidenceStatusCode === StatusCode.PENDING;
    this.accessRequestFailed = false;
    this.hasCaseListingResults = false;
    this.isCaseFound = false;
    this.requestedCaseNotFound = false;
    this.isCaseSearchInProgress = false;
    this.isFindDisabled = true;
    this.formControlNames = ['ParticipantId', 'CaseListing'];

    // get current case requests
    this.getPartyRequests();
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public checkCaseInput(): void {
    this.isFindDisabled = this.formState.caseName.value.length < 6;
  }

  public onRemoveCase(requestedCase: DigitalEvidenceCaseRequest): void {
    requestedCase.status = CaseStatus.RemoveRequested;
    const data: DialogOptions = {
      title: 'Remove case access',
      component: HtmlComponent,
      data: {
        content: `You are about request removal from case ${requestedCase.agencyFileNumber}. Continue?`,
      },
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .pipe(
        exhaustMap((result) =>
          result
            ? this.digitalEvidenceCaseResource.removeCaseRequest(requestedCase)
            : EMPTY
        )
      )
      .subscribe();
  }

  public showFormControl(formControlName: string): boolean {
    return this.formControlNames.includes(formControlName);
  }

  public onChangeAgencyCode(): void {
    alert(this.formState.agencyCode.value);
  }

  public getPartyRequests(): void {
    this.digitalEvidenceCaseResource
      .getPartyCaseRequests(this.partyService.partyId)
      .pipe()
      .subscribe(
        (digitalEvidenceCaseRequests: DigitalEvidenceCaseRequest[]) => {
          this.caseListing = digitalEvidenceCaseRequests;
          this.dataSource.data = this.caseListing;
        }
      );
  }

  public findCase(): void {
    this.requestedCaseNotFound = false;
    this.digitalEvidenceCaseResource
      .findCase(this.formState.agencyCode.value, this.formState.caseName.value)
      .pipe(
        tap(() => {
          this.isCaseFound = true;
          this.isCaseSearchInProgress = false;
        })
      )
      .subscribe((digitalEvidenceCase: DigitalEvidenceCase | null) => {
        this.requestedCaseNotFound = !digitalEvidenceCase ? true : false;
        this.requestedCase = digitalEvidenceCase;
      });
  }
  protected performSubmission(): Observable<unknown> {
    throw new Error('Method not implemented.');
  }
  public addCaseRequest(): void {
    if (this.requestedCase !== null) {
      // check case not already requested
      const idExists = this.caseListing.some(
        (caseEntry) =>
          caseEntry.id === this.requestedCase?.id &&
          caseEntry.status !== CaseStatus.RemoveRequested
      );

      if (idExists) {
        this.toastService.openErrorToast('Case already requested');
      } else {
        this.onRequestAccess();
      }
    }
  }

  public getCaseAttribute(fieldId: number): string {
    return (
      this.requestedCase?.fields?.find((c) => c.id === fieldId)?.value ||
      'Not set'
    );
  }

  public onRequestAccess(): void {
    if (this.requestedCase !== null) {
      const agencyFileNumber = this.requestedCase.fields.find(
        (c) => c.name === 'Agency File No.'
      )?.value;
      this.resource
        .requestAccess(
          this.partyService.partyId,
          this.requestedCase?.id,
          agencyFileNumber
        )
        .pipe(
          tap(() => this.getPartyRequests()),
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

    // this.formState.agencyCode.patchValue(this.partyService.partyId);

    this.formState.agencyCode.patchValue('105');

    this.formState.caseName.valueChanges
      .pipe(
        debounceTime(400),
        filter((value: string) => value.length > 12),
        switchMap((value: string) => {
          this.isCaseSearchInProgress = true;
          this.isCaseFound = false;

          return this.digitalEvidenceCaseResource.findCase(
            this.formState.agencyCode.value,
            value
          );
        }),
        tap((digitalEvidenceCase: any) => {
          // Set a value on completion of the observable
          // For example, you could set a boolean flag to indicate completion
          console.log(digitalEvidenceCase);
          this.isCaseFound = true;

          this.isCaseSearchInProgress = false;
        })
      )
      .subscribe((digitalEvidenceCase: DigitalEvidenceCase | null) => {
        // Do something with the search results here
        this.requestedCase = digitalEvidenceCase;
        console.log(digitalEvidenceCase);
      });
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}
