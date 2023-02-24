import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, map, of, throwError } from 'rxjs';

import {
  AbstractResource,
  NoContent,
  NoContentResponse,
} from '@bcgov/shared/data-access';

import { LoggerService } from '@app/core/services/logger.service';
import { ToastService } from '@app/core/services/toast.service';

import { DemsAccount } from '../digital-evidence-account.model';
import { DigitalEvidenceCaseManagementResource } from './digital-evidence-case-management-resource.service';
import { DigitalEvidenceCase } from './digital-evidence-case.model';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceCaseResource extends AbstractResource {
  public constructor(
    private toastService: ToastService,
    private logger: LoggerService,
    private apiResource: DigitalEvidenceCaseManagementResource
  ) {
    super('case-management');
  }

  public requestAccess(
    partyId: number,
    caseId: number,
    agencyFileNumber: string
  ): NoContent {
    return this.apiResource
      .requestAccess(partyId, caseId, agencyFileNumber)
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          if (error.status === HttpStatusCode.BadRequest) {
            return of(void 0);
          }

          return throwError(() => error);
        })
      );
  }

  public findCase(
    agencyCode: string,
    caseName: string
  ): Observable<DigitalEvidenceCase> {
    return this.apiResource.findCase(agencyCode, caseName).pipe(
      map((response: DigitalEvidenceCase) => response),
      catchError((error: HttpErrorResponse) => {
        const message =
          error.status === 404
            ? `Case not found '${caseName}'`
            : 'Unable to find cases';
        this.toastService.openErrorToast(message);
        this.logger.error(
          '[DigitalEvidenceCaseFindResponse::find] error has occurred: ',
          error
        );
        return throwError(error);
      })
    );
  }
}
