import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, throwError } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';
import { ResourceUtilsService } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';

import { DemsAccount } from '../digital-evidence-account.model';
import { DigitalEvidenceCaseFindResponse } from './digital-evidence-case-find-response.model';
import { DigitalEvidenceCase } from './digital-evidence-case.model';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceCaseManagementResource {
  public constructor(
    private apiResource: ApiHttpClient,
    private portalResource: PortalResource,
    private resourceUtilsService: ResourceUtilsService
  ) {}

  public getProfileStatus(partyId: number): Observable<ProfileStatus | null> {
    return this.portalResource.getProfileStatus(partyId);
  }

  public findCase(
    agencyCode: string,
    caseName: string
  ): Observable<DigitalEvidenceCase> {
    const search = agencyCode + ': ' + caseName;
    return this.apiResource.get(`digital-evidence-cases/${search}`);
  }

  public requestAccess(
    partyId: number,
    caseId: number,
    agencyFileNumber: string
  ): NoContent {
    return this.apiResource
      .post<NoContent>('digital-evidence-cases', {
        partyId,
        caseId,
        agencyFileNumber,
      })
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          return throwError(() => error);
        })
      );
  }
}
