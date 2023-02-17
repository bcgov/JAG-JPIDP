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
  map,
  noop,
  of,
  switchMap,
  tap,
} from 'rxjs';

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
import { DigitalEvidenceCaseAutocompleteResource } from './digital-evidence-case-autocomplete-resource.service';
import { DigitalEvidenceCaseAutocompleteResponse } from './digital-evidence-case-autocomplete-response.model';
import { DigitalEvidenceCaseManagementFormState } from './digital-evidence-case-management-form.state';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';
import { CaseStatus, DigitalEvidenceCase } from './digital-evidence-case.model';

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
  public caseListing: DigitalEvidenceCase[] = [];

  public identityProvider$: Observable<IdentityProvider>;
  //public userType$: Observable<OrganizationUserType[]>;
  public IdentityProvider = IdentityProvider;

  public collectionNotice: string;
  public completed: boolean | null;
  public pending: boolean | null;
  public policeAgency: Observable<string>;
  public result: string;
  public isCaseSearchInProgress: boolean;
  public accessRequestFailed: boolean;
  public hasCaseListingResults: boolean;
  //@Input() public form!: FormGroup;
  public formControlNames: string[];
  public selectedOption = 0;
  public dataSource: MatTableDataSource<DigitalEvidenceCase>;
  public displayedColumns: string[] = [
    'caseNumber',
    'name',
    'requestedDate',
    'assignedDate',
    'status',
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
    private digitalEvidenceCaseAutocompleteResource: DigitalEvidenceCaseAutocompleteResource,
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
    this.isCaseSearchInProgress = false;
    this.formControlNames = ['ParticipantId', 'CaseListing'];
  }

  public onBack(): void {
    this.navigateToRoot();
  }
  protected performSubmission(): Observable<void> {
    const partyId = this.partyService.partyId;
    console.log('Submiting');
    console.log(this.formState.ParticipantId.value);
    if (this.selectedOption == 1) {
      return partyId && this.formState.json
        ? this.resource.requestAccess(partyId)
        : EMPTY;
    }

    return partyId && this.formState.json
      ? this.resource.requestAccess(partyId)
      : EMPTY;
  }

  public onCaseIdChange(): void {
    if (this.formState.caseId.value.length >= 3) {
      alert(this.formState.caseId.value);
      this.isCaseSearchInProgress = true;
    }
  }

  public showFormControl(formControlName: string): boolean {
    return this.formControlNames.includes(formControlName);
  }
  // public get userType(): FormControl {
  //   return this.form.get('userType') as FormControl;
  // }
  public onAutocomplete(input: string): void {
    this.digitalEvidenceCaseAutocompleteResource
      .searchCases(input)
      .subscribe((results) => {
        this.toastService.openErrorToast(
          'Cases could not be retrieved ' + results
        );
      });
  }
  public onChangeAgencyCode(): void {
    alert(this.formState.agencyCode.value);
  }

  // public OnSubmit(): void {
  //   console.log('Form Submitted');
  //   console.log(this.form.value);
  // }
  public onRequestAccess(): void {
    console.log('Submiting');
    console.log(this.formState.ParticipantId.value);
    if (this.selectedOption == 1) {
      this.resource
        .requestAccess(this.partyService.partyId)
        .pipe(
          tap(() => (this.completed = true)),
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
        .requestAccess(this.partyService.partyId)
        .pipe(
          tap(() => (this.completed = true)),
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

    this.formState.agencyCode.patchValue(this.partyService.partyId);

    const sample1: DigitalEvidenceCase = {
      requestedDate: new Date(),
      assignedDate: new Date(),
      name: '401: 23-23232',
      status: CaseStatus.Active,
      caseNumber: '401: 23-23232',
    };
    const sample2: DigitalEvidenceCase = {
      requestedDate: new Date(),
      assignedDate: new Date(),
      name: '401: 23-5544',
      status: CaseStatus.Pending,
      caseNumber: '401: 23-5544',
    };

    this.caseListing = [sample1, sample2];
    this.dataSource.data = this.caseListing;

    this.formState.caseId.valueChanges
      .pipe(
        debounceTime(400),
        switchMap((value: string) => {
          return value
            ? this.digitalEvidenceCaseAutocompleteResource.searchCases(value)
            : EMPTY;
        })
      )
      .subscribe();
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}
