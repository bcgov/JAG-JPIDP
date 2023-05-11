import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, map, of, throwError } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';
import { ResourceUtilsService } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';

import {
  CourtLocation,
  CourtLocationRequest,
} from './digital-evidence-counsel-model';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceCounselResource {
  public constructor(
    private apiResource: ApiHttpClient,
    private portalResource: PortalResource,
    private resourceUtilsService: ResourceUtilsService
  ) {}

  public getProfileStatus(partyId: number): Observable<ProfileStatus | null> {
    return this.portalResource.getProfileStatus(partyId);
  }

  public getLocations(): Observable<CourtLocation[]> {
    return this.apiResource.get(`court-location`, {});
  }

  public requestLocationAccess(
    request: CourtLocationRequest
  ): Observable<CourtLocationRequest | null> {
    return this.apiResource
      .post<CourtLocationRequest>(`court-location`, request)
      .pipe(
        map((response: CourtLocationRequest) => response),
        catchError((error: HttpErrorResponse) => {
          console.error('Error %o', error);
          throw error;
        })
      );
  }

  public getLocationAccessRequests(
    partyId: number,
    includeDeleted: boolean
  ): Observable<CourtLocationRequest[]> {
    return this.apiResource.get(`court-location/party/${partyId}/requests`, {
      params: {
        includeDeleted: includeDeleted,
      },
    });
  }

  public removeCaseAccessRequest(
    partyId: number,
    requestId: number | undefined
  ): NoContent {
    return this.apiResource
      .delete<NoContent>(
        `court-location/party/${partyId}/request/${requestId}`,
        {}
      )
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          return throwError(() => error);
        })
      );
  }
}
