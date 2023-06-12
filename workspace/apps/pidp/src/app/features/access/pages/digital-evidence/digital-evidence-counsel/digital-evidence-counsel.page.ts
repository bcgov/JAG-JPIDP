import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  Inject,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { DateAdapter, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, Observable, catchError, exhaustMap, noop, of } from 'rxjs';

import {
  ConfirmDialogComponent,
  DialogOptions,
  HtmlComponent,
} from '@bcgov/shared/ui';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { FormUtilsService } from '@app/core/services/form-utils.service';
import { ToastService } from '@app/core/services/toast.service';

import { DigitalEvidenceCounselFormState } from './digital-evidence-counsel-form-state';
import {
  CourtLocation,
  CourtLocationRequest,
  CourtRequestStatus,
} from './digital-evidence-counsel-model';
import { DigitalEvidenceCounselResource } from './digital-evidence-counsel-resource.service';

export const CUSTOM_DATE_FORMATS = {
  parse: {
    dateInput: 'DD-MM-YYYY',
  },
  display: {
    dateInput: 'MMM DD, YYYY',
    monthYearLabel: 'MMMM YYYY',
    dateA11yLabel: 'LL',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

@Component({
  selector: 'app-digital-evidence-counsel',
  templateUrl: './digital-evidence-counsel.page.html',
  styleUrls: ['./digital-evidence-counsel.page.scss'],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'en-GB' }],
})
export class DigitalEvidenceCounselPage
  extends AbstractFormPage<DigitalEvidenceCounselFormState>
  implements OnInit, AfterViewInit
{
  public formState: DigitalEvidenceCounselFormState;
  public title: string;
  public minDate: Date = new Date();
  public maxDate: Date = new Date(this.minDate);
  private MAX_DAYS_OUT = 90;
  public courtLocations!: CourtLocation[];
  public filteredOptions!: CourtLocation[];
  public formControlNames: string[];
  public selectedLocation?: CourtLocation;
  public dataSource: MatTableDataSource<CourtLocationRequest>;
  public courtListing: CourtLocationRequest[] = [];
  public displayedColumns: string[] = [
    'courtLocation',
    'requestedOn',
    'validFrom',
    'validUntil',
    'requestStatus',
    'action',
  ];
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  @ViewChild('courtTblSortWithObject') sort = new MatSort();

  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private resource: DigitalEvidenceCounselResource,
    private partyService: PartyService,
    private toastService: ToastService,
    private dateAdapter: DateAdapter<Date>,
    fb: FormBuilder
  ) {
    super(dialog, formUtilsService);
    const routeData = this.route.snapshot.data;
    resource.getLocations().subscribe((data) => {
      this.courtLocations = data;
    });
    this.dateAdapter.setLocale('en-GB'); //dd/MM/yyyy

    this.title = routeData.title;
    this.formState = new DigitalEvidenceCounselFormState(fb);
    this.maxDate.setDate(this.minDate.getDate() + this.MAX_DAYS_OUT);
    this.formControlNames = [
      'courtLocation',
      'dateFrom',
      'dateTo',
      'showDeleted',
    ];
    this.dataSource = new MatTableDataSource(this.courtListing);
  }

  public ngOnInit(): void {
    this.resource.getLocations().subscribe((locations) => {
      this.courtLocations = locations;
      this.filteredOptions = locations;
    });

    this.formState.courtLocation.valueChanges.subscribe((value: any) => {
      if (typeof value === 'string') {
        const filterValue = value.toLowerCase();
        this.filteredOptions = this.courtLocations.filter((option) =>
          option.name.toLowerCase().includes(filterValue)
        );
      }
    });

    this.formState.showDeleted.valueChanges.subscribe(() => {
      this.loadExistingRequests();
    });

    this.loadExistingRequests();
  }

  public getLocationDisplay(value?: CourtLocation): any {
    if (value) return value.name;
    else return '';
  }

  public ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
  }

  public loadExistingRequests(): void {
    const partyId = this.partyService.partyId;

    // get existing requests
    this.resource
      .getLocationAccessRequests(
        partyId,
        this.formState.showDeleted.value || false
      )
      .subscribe((results: CourtLocationRequest[]) => {
        this.courtListing = results;
        // Create a new instance of MatTableDataSource with the loaded data
        this.dataSource = new MatTableDataSource(this.courtListing);
        // Set the sorting and pagination properties of the dataSource
        this.dataSource.data = this.courtListing;
        this.dataSource.sort = this.sort;
      });
  }

  public formComplete(): boolean {
    return (
      this.formState.courtLocation.valid &&
      this.formState.dateFrom.valid &&
      this.formState.dateTo.valid
    );
  }

  public requestAccess(): void {
    const dateFrom = new Date(this.formState.dateFrom.value);
    const dateTo = new Date(this.formState.dateTo.value);
    const partyId = this.partyService.partyId;

    const locationRequest: CourtLocationRequest = {
      partyId: partyId,
      validFrom: dateFrom,
      validUntil: dateTo,
      requestStatus: CourtRequestStatus.NewRequest,
      courtLocation: this.formState.courtLocation.value,
    };
    this.resource
      .requestLocationAccess(locationRequest)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status === HttpStatusCode.NotFound) {
            this.navigateToRoot();
          }

          const message = error.error?.detail
            ? error.error.detail
            : error.message;

          this.toastService.openErrorToast('Failed to add request: ' + message);
          return of(noop());
        })
      )
      .subscribe(() => {
        this.clearLocation();
        this.formState.reset();
        this.loadExistingRequests();
      });
  }

  public onRemoveAccess(request: CourtLocationRequest): void {
    this.toastService.openInfoToast('Removing ' + request.requestId);
    const partyId = this.partyService.partyId;

    const data: DialogOptions = {
      title: 'Remove location access',
      component: HtmlComponent,
      data: {
        content: `You are about request removal from court location ${request.courtLocation.name}. Continue?`,
      },
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .pipe(
        exhaustMap((result) =>
          result
            ? this.resource.removeCaseAccessRequest(partyId, request.requestId)
            : EMPTY
        )
      )
      .subscribe({
        complete: () => {
          this.loadExistingRequests();
        },
      });
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public clearLocation(): void {
    this.formState.courtLocation.patchValue('');
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }

  public onSelectLocation($event: MatAutocompleteSelectedEvent): void {
    console.log('Selected %o', $event);
  }

  protected performSubmission(): Observable<unknown> {
    throw new Error('Method not implemented.');
  }
}
