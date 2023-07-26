import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, throwError } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';
import { ResourceUtilsService } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';

import {
  DigitalEvidenceCase,
  DigitalEvidenceCaseAccessRequest,
  DigitalEvidenceCaseRequest,
} from './digital-evidence-case.model';

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

  public removeCaseAccessRequest(requestId: number): NoContent {
    return this.apiResource
      .put<NoContent>(`evidence-case-management/${requestId}`, {})
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          return throwError(() => error);
        })
      );
  }

  public getPartyCaseRequests(
    partyId: number
  ): Observable<DigitalEvidenceCaseRequest[]> {
    return this.apiResource.get(`evidence-case-management`, {
      params: {
        partyId: partyId,
      },
    });
  }

  public getCaseInfo(caseID: number): Observable<DigitalEvidenceCase> {
    return this.apiResource.get(`evidence-case-management/case/${caseID}`, {});
  }

  public findCase(
    agencyCode: string,
    caseName: string
  ): Observable<DigitalEvidenceCase> {
    const search = agencyCode + ': ' + caseName;
    return this.apiResource.get(`evidence-case-management/search`, {
      params: {
        AgencyFileNumber: search,
      },
    });
  }

  public requestAccess(request: DigitalEvidenceCaseAccessRequest): NoContent {
    return this.apiResource
      .post<NoContent>('evidence-case-management', request)
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          return throwError(() => error);
        })
      );
  }
}
