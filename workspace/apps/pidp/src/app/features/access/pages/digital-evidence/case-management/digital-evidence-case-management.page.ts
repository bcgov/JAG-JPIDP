import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  Inject,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import {
  EMPTY,
  Observable,
  catchError,
  exhaustMap,
  interval,
  map,
  noop,
  of,
  retry,
  takeWhile,
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
import { DigitalEvidenceCaseManagementFormState } from './digital-evidence-case-management-form.state';
import { DigitalEvidenceCaseManagementInfoDialogComponent } from './digital-evidence-case-management-info-dialog';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';
import {
  CaseStatus,
  DigitalEvidenceCase,
  DigitalEvidenceCaseAccessRequest,
  DigitalEvidenceCaseRequest,
} from './digital-evidence-case.model';

@Component({
  selector: 'app-digital-evidence',
  templateUrl: './digital-evidence-case-management.page.html',
  styleUrls: ['./digital-evidence-case-management.page.scss'],
})
export class DigitalEvidenceCaseManagementPage
  extends AbstractFormPage<DigitalEvidenceCaseManagementFormState>
  implements OnInit, AfterViewInit
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
  public pageSize: number;
  public pageIndex: number;
  public requestedCase!: DigitalEvidenceCase | null;
  public isCaseSearchInProgress: boolean;
  public showAUFLink: boolean;
  public showCaseImportLink: boolean;
  public showCaseToolsLink: boolean;

  public isCaseFound: boolean;
  public accessRequestFailed: boolean;
  public showJUSTINCaseInfo: boolean;
  public requestedCaseNotFound: boolean;
  public isFindDisabled: boolean;
  public refreshEnabled: boolean;
  public requestedCaseInactive: boolean;
  public checkingQueuedCase: boolean;
  public checkingQueuedCaseInProgress: boolean;

  public launchDEMSLabel: string;
  public accessPoliceToolsCaseLabel: string;

  public hasCaseListingResults: boolean;
  public caseTooltip: string;
  public refreshCount: number;
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
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  @ViewChild('caseTblSortWithObject') sort = new MatSort();
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private partyService: PartyService,
    private resource: DigitalEvidenceCaseManagementResource,
    private usertype: PartyUserTypeResource,
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
    const AGENCY_CODE = 'agencyCode';
    this.title = routeData.title;
    this.organizationType = new OrganizationUserType();
    const partyId = this.partyService.partyId;
    this.dataSource = new MatTableDataSource(this.caseListing);
    this.identityProvider$ = this.authorizedUserService.identityProvider$;
    this.result = '';
    this.caseTooltip = 'test';
    this.isFindDisabled = true;
    this.policeAgency = accessTokenService
      .decodeToken()
      .pipe(map((token) => token?.identity_provider ?? ''));
    this.showAUFLink = this.config.caseManagement.showAUFLink;
    this.showCaseImportLink = this.config.caseManagement.showCaseImportLink;
    this.showCaseToolsLink = this.config.caseManagement.showCaseToolsLink;
    this.launchDEMSLabel = this.config.launch.subAgencyAufPortalLabel;
    this.accessPoliceToolsCaseLabel =
      this.config.launch.policeToolsCaseAccessLabel;

    accessTokenService.decodeToken().subscribe((n) => {
      if (n !== null) {
        this.result = n.identity_provider;
      }
    });

    this.formState = new DigitalEvidenceCaseManagementFormState(fb);

    this.usertype.getUserType(partyId).subscribe((data: any) => {
      this.organizationType.organizationType = data['organzationType'];
      this.organizationType.participantId = data['participantId'];
      this.organizationType.organizationName = data['organizationName'];
      this.organizationType.submittingAgencyCode = data['submittingAgencyCode'];

      // sticky agency codes if org in the sticky list
      if (
        this.organizationType.submittingAgencyCode &&
        this.config.caseManagement.stickyAgencyCodes.includes(
          this.organizationType.submittingAgencyCode
        )
      ) {
        // no local code set but we have the agency code
        if (
          !localStorage.getItem(AGENCY_CODE) &&
          this.organizationType.submittingAgencyCode
        ) {
          if (
            this.organizationType.submittingAgencyCode &&
            this.formState.agencyCode.value
          ) {
            localStorage.setItem(
              AGENCY_CODE,
              this.organizationType.submittingAgencyCode
            );
          }
        } else if (localStorage.getItem(AGENCY_CODE)) {
          this.organizationType.submittingAgencyCode =
            localStorage.getItem(AGENCY_CODE) || '';
          this.formState.agencyCode.patchValue(
            this.organizationType.submittingAgencyCode
          );
        }
      } else {
        this.formState.agencyCode.patchValue(
          this.organizationType.submittingAgencyCode
        );
      }
    });
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
    this.showJUSTINCaseInfo = false;
    this.requestedCaseInactive = false;
    this.refreshEnabled = false;
    this.checkingQueuedCase = false;
    this.checkingQueuedCaseInProgress = false;
    this.refreshCount = 0;
    this.pageSize = 20;
    this.pageIndex = 0;
    this.formControlNames = ['caseName', 'agencyCode'];

    // get current case requests
    this.getPartyRequests();
  }

  public ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public getCaseData(row: DigitalEvidenceCaseRequest): void {
    this.resource
      .getCaseInfo(row.id)
      .subscribe((response: DigitalEvidenceCase) => {
        this.showInfoDialog(response);
      });
  }

  public showInfoDialog(data: DigitalEvidenceCase): void {
    this.dialog.open(DigitalEvidenceCaseManagementInfoDialogComponent, {
      data: data,
    });
  }

  public checkCaseInput(): boolean {
    if (this.formState.caseName.value)
      this.formState.caseName.setValue(this.formState.caseName.value.trim());

    this.checkingQueuedCaseInProgress = false;
    this.isFindDisabled =
      this.formState.caseName.value &&
      this.formState.caseName?.value.length >= 4 &&
      this.formState.agencyCode.value &&
      this.formState.agencyCode.value.length >= 2
        ? false
        : true;

    return this.isFindDisabled;
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
            ? this.digitalEvidenceCaseResource.removeCaseAccessRequest(
                requestedCase.requestId
              )
            : EMPTY
        )
      )
      .subscribe({
        complete: () => {
          this.refreshEnabled = true;
          this.refreshCount = 0;
          this.getPartyRequests();
        },
      });
  }

  public showFormControl(formControlName: string): boolean {
    return this.formControlNames.includes(formControlName);
  }

  public getPartyRequests(): void {
    this.digitalEvidenceCaseResource
      .getPartyCaseRequests(this.partyService.partyId)
      .pipe()
      .subscribe(
        (digitalEvidenceCaseRequests: DigitalEvidenceCaseRequest[]) => {
          // if eveything complete then quit refreshing
          this.refreshEnabled = digitalEvidenceCaseRequests.some(
            (caseRequest) => caseRequest.requestStatus !== CaseStatus.Completed
          );

          this.caseListing = digitalEvidenceCaseRequests;
          // Create a new instance of MatTableDataSource with the loaded data
          this.dataSource = new MatTableDataSource(this.caseListing);
          // Set the sorting and pagination properties of the dataSource
          this.dataSource.data = this.caseListing;
          this.dataSource.sort = this.sort;

          this.dataSource.sortingDataAccessor = (
            row: DigitalEvidenceCaseRequest,
            columnName: string
          ): string => {
            const columnValue =
              row[columnName as keyof DigitalEvidenceCaseRequest];
            return columnValue?.toString() ?? '';
          };
          this.refreshTable();
          if (this.refreshEnabled) {
            this.refreshCount++;
            if (this.refreshCount >= 3) {
              this.refreshEnabled = false;
              this.refreshCount = 0;
            }
          }
        }
      );
  }

  public launchAUF(): void {
    this.openPopUp(this.config.demsImportURL);
  }

  public hasToolsCaseAccess(): boolean {
    return this.caseListing.some(
      (c) => c.agencyFileNumber === 'AUF Tools Case'
    );
  }

  public requestToolsCaseAccess(): void {
    this.refreshEnabled = true;

    const accessRequest: DigitalEvidenceCaseAccessRequest = {
      partyId: this.partyService.partyId,
      toolsCaseRequest: true,
      caseId: -1,
      agencyFileNumber: '',
      key: '',
      name: '',
    };

    this.resource
      .requestAccess(accessRequest)
      .pipe(
        tap(() => this.getPartyRequests()),
        catchError((error: HttpErrorResponse) => {
          if (error.status === HttpStatusCode.NotFound) {
            this.navigateToRoot();
          }
          this.toastService.openErrorToast(
            'Failed to add case request :' + error.message
          );
          this.accessRequestFailed = true;
          return of(noop());
        })
      )
      .subscribe(() => {
        this.formState.caseName.patchValue('');
        this.formState.agencyCode.patchValue(
          this.organizationType.submittingAgencyCode
        );
        this.requestedCase = null;
        this.refreshCount = 0;
        this.refreshTable();
      });
  }

  public findCase(): void {
    if (this.isCaseSearchInProgress) {
      return;
    }

    if (
      this.organizationType.submittingAgencyCode &&
      this.config.caseManagement.stickyAgencyCodes.includes(
        this.organizationType.submittingAgencyCode
      )
    ) {
      if (this.formState.agencyCode.value) {
        localStorage.setItem('agencyCode', this.formState.agencyCode.value);
      }
    }

    this.requestedCase = null;
    this.showJUSTINCaseInfo = false;
    this.requestedCaseNotFound = false;
    this.requestedCaseInactive = false;
    this.isCaseSearchInProgress = true;

    this.digitalEvidenceCaseResource
      .findCase(this.formState.agencyCode.value, this.formState.caseName.value)
      .pipe(
        retry(0),
        catchError((error) => {
          if (error.status === 500) {
            this.toastService.openErrorToast(
              'Case searching failed - please retry or contact support'
            );
          }
          if (error.status === 404) {
            this.isCaseFound = false;
            this.requestedCaseNotFound = true;
          }

          this.isCaseSearchInProgress = false;

          throw error;
        })
      )
      .subscribe((digitalEvidenceCase: DigitalEvidenceCase) => {
        this.isCaseFound = true;
        this.requestedCaseNotFound =
          !digitalEvidenceCase || digitalEvidenceCase.status === 'NotFound'
            ? true
            : false;
        if (
          digitalEvidenceCase?.status === 'Inactive' &&
          !digitalEvidenceCase?.justinStatus
        ) {
          this.requestedCaseInactive = true;
        }

        if (digitalEvidenceCase?.status === 'Queued') {
          this.checkingQueuedCase = true;
          if (!this.checkingQueuedCaseInProgress) {
            this.recheckCase();
          }
        }

        if (
          digitalEvidenceCase?.status !== 'Active' &&
          digitalEvidenceCase?.status !== 'Queued' &&
          digitalEvidenceCase?.justinStatus
        ) {
          this.showJUSTINCaseInfo = true;
        }

        if (
          digitalEvidenceCase?.status === 'Active' &&
          this.checkingQueuedCase
        ) {
          this.toastService.openInfoToast('Case is now available');
          this.checkingQueuedCaseInProgress = false;
          this.checkingQueuedCase = false;
        }

        this.requestedCase = digitalEvidenceCase;
        this.isCaseSearchInProgress = false;
      });
  }

  public cancelQueuedSearch(): void {
    this.checkingQueuedCase = false;
    this.checkingQueuedCaseInProgress = false;
    this.requestedCase = null;
  }

  public decodeName(name: string): string {
    return decodeURIComponent(name);
  }

  public getCleanCaseName(): string {
    const caseName = this.requestedCase
      ? this.requestedCase.name.replace('+', '')
      : '';
    return caseName;
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
        this.toastService.openErrorToast('Case already in active list');
      } else {
        this.onRequestAccess();
      }
    }
  }

  public refreshTable(): void {
    interval(8000)
      .pipe(takeWhile(() => this.refreshEnabled))
      .subscribe(() => {
        this.getPartyRequests();
      });
  }

  public recheckCase(): void {
    this.checkingQueuedCaseInProgress = true;
    interval(5000)
      .pipe(takeWhile(() => this.checkingQueuedCase))
      .subscribe(() => {
        this.findCase();
      });
  }

  public getCaseAttribute(fieldId: number): string {
    return (
      this.requestedCase?.fields?.find((c) => c.id === fieldId)?.value ||
      'Not set'
    );
  }

  public onUploadToCase(evidenceCase: DigitalEvidenceCase): void {
    let url = this.config.demsImportURL;

    if (this.config.demsImportURL.indexOf('~~CASEID~~') !== -1) {
      url = url.replace('~~CASEID~~', '' + evidenceCase.id);
    } else {
      url += evidenceCase.id;
    }
    this.openPopUp(url);
  }

  public openPopUp(urlToOpen: string): void {
    const popup_window = window.open(
      urlToOpen,
      'aufWindow',
      'toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, copyhistory=yes'
    );
    try {
      popup_window?.focus();
    } catch (e) {
      this.toastService.openErrorToast(
        'Popup blocked enabled - please add ' +
          this.config.demsImportURL +
          ' to your exception list'
      );
    }
  }

  public onRequestAccess(): void {
    if (this.requestedCase !== null) {
      this.refreshEnabled = true;
      const agencyFileNumber = this.requestedCase.fields.find(
        (c) => c.name === 'Agency File No.'
      )?.value;

      const accessRequest: DigitalEvidenceCaseAccessRequest = {
        partyId: this.partyService.partyId,
        toolsCaseRequest: false,
        agencyFileNumber: agencyFileNumber,
        caseId: this.requestedCase.id,
        name: this.requestedCase.name,
        key: this.requestedCase.key,
      };

      this.resource
        .requestAccess(accessRequest)
        .pipe(
          tap(() => this.getPartyRequests()),
          catchError((error: HttpErrorResponse) => {
            if (error.status === HttpStatusCode.NotFound) {
              this.navigateToRoot();
            }
            this.toastService.openErrorToast(
              'Failed to add case request :' + error.message
            );
            this.accessRequestFailed = true;
            return of(noop());
          })
        )
        .subscribe(() => {
          this.formState.caseName.patchValue('');
          this.formState.agencyCode.patchValue(
            this.organizationType.submittingAgencyCode
          );
          this.requestedCase = null;
          this.refreshCount = 0;
          this.refreshTable();
        });
    }
  }

  public ngOnInit(): void {
    const partyId = this.partyService.partyId;
    this.dataSource.paginator = this.paginator;

    this.refreshTable();

    if (!partyId) {
      this.logger.error('No party ID was provided');
      return this.navigateToRoot();
    }

    if (this.completed === null) {
      this.logger.error('No status code was provided');
      return this.navigateToRoot();
    }
  }

  public isWithin25Days(requestedOnStr: string): boolean {
    const now = new Date();
    const requestedOn = new Date(requestedOnStr);
    const twentyfiveDaysInMilliseconds = 25 * 24 * 60 * 60 * 1000;
    const twentyFiveDaysAgo = new Date(
      now.getTime() - twentyfiveDaysInMilliseconds
    );
    return requestedOn <= twentyFiveDaysAgo;
  }

  public onPaginationChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    // fetch data for the current page using slice method
    const startIndex = this.pageIndex * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.dataSource.data = this.caseListing.slice(startIndex, endIndex);
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}
