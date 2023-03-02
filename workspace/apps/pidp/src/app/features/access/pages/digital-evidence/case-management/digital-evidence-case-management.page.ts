import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  Inject,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormBuilder, } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort} from '@angular/material/sort';
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
import { BcpsAuthResourceService } from '../auth/bcps-auth-resource.service';
import { DigitalEvidenceCaseManagementFormState } from './digital-evidence-case-management-form.state';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';
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
  public isCaseFound: boolean;
  public accessRequestFailed: boolean;
  public requestedCaseNotFound: boolean;
  public isFindDisabled: boolean;
  public refreshEnabled: boolean;
  public requestedCaseInactive: boolean;
  public hasCaseListingResults: boolean;
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
    this.title = routeData.title;
    this.organizationType = new OrganizationUserType();
    const partyId = this.partyService.partyId;
    this.dataSource = new MatTableDataSource(this.caseListing);
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
      this.organizationType.organizationType = data['organzationType'];
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
    this.requestedCaseInactive = false;
    this.refreshEnabled = false;
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

  public findCase(): void {
    this.requestedCaseNotFound = false;
    this.requestedCaseInactive = false;
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
        if (digitalEvidenceCase?.status !== 'Active') {
          this.requestedCaseInactive = true;
        }
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

  public getCaseAttribute(fieldId: number): string {
    return (
      this.requestedCase?.fields?.find((c) => c.id === fieldId)?.value ||
      'Not set'
    );
  }

  public onRequestAccess(): void {
    if (this.requestedCase !== null) {
      this.refreshEnabled = true;
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
        .subscribe(() => {
          this.formState.caseName.patchValue('');
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

    // this.formState.agencyCode.patchValue(this.partyService.partyId);

    this.formState.agencyCode.patchValue('105');
  }

  public isWithin25Days(requestedOn: Date): boolean {
    const now = new Date();
    const twentyfiveDaysInMilliseconds = 5 * 24 * 60 * 60 * 1000;
    const twentyFiveDaysAgo = new Date(
      now.getTime() - twentyfiveDaysInMilliseconds
    );
    return requestedOn >= twentyFiveDaysAgo && requestedOn <= now;
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
