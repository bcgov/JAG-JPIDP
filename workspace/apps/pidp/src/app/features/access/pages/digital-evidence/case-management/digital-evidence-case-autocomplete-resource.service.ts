import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, map, of } from 'rxjs';

import { AbstractResource } from '@bcgov/shared/data-access';

import { LoggerService } from '@app/core/services/logger.service';
import { ToastService } from '@app/core/services/toast.service';

import { DigitalEvidenceCaseAutocompleteResponse } from './digital-evidence-case-autocomplete-response.model';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceCaseAutocompleteResource extends AbstractResource {
  public constructor(
    private toastService: ToastService,
    private logger: LoggerService,
    private apiResource: DigitalEvidenceCaseManagementResource
  ) {
    super('address-autocomplete');
  }

  public searchCases(
    searchTerm: string
  ): Observable<DigitalEvidenceCaseAutocompleteResponse[]> {
    return this.apiResource.searchCases(searchTerm).pipe(
      map(
        (response: DigitalEvidenceCaseAutocompleteResponse[]) => response ?? []
      ),
      catchError((error: HttpErrorResponse) => {
        const message =
          error.status === 404
            ? `No cases found for search term '${searchTerm}'`
            : 'Unable to find cases';
        this.toastService.openErrorToast(message);

        this.logger.error(
          '[DigitalEvidenceCaseAutocompleteResponse::find] error has occurred: ',
          error
        );

        return of([]);
      })
    );
  }
}
