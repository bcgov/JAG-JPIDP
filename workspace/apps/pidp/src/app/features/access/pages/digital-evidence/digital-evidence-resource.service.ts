import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, of, throwError } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';

import { DigitalEvidenceCase } from './case-management/digital-evidence-case.model';
import { DemsAccount } from './digital-evidence-account.model';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceResource {
  public constructor(
    private apiResource: ApiHttpClient,
    private portalResource: PortalResource
  ) {}

  public getProfileStatus(partyId: number): Observable<ProfileStatus | null> {
    return this.portalResource.getProfileStatus(partyId);
  }

  public validateDefenceId(
    partyID: number,
    defenceID: string
  ): Observable<DigitalEvidenceCase> {
    return this.apiResource.get(`defence-counsel/${partyID}/${defenceID}`);
  }

  public requestDefenceCounselAccess(
    partyId: number,
    organizationType: DemsAccount,
    organizationName: DemsAccount,
    participantId: DemsAccount
  ): NoContent {
    return this.apiResource
      .post<NoContent>('access-requests/digital-evidence-defence', {
        partyId,
        organizationType,
        organizationName,
        participantId,
      })
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

  public requestAccess(
    partyId: number,
    organizationType: DemsAccount,
    organizationName: DemsAccount,
    participantId: DemsAccount,
    assignedRegions: DemsAccount
  ): NoContent {
    return this.apiResource
      .post<NoContent>('access-requests/digital-evidence', {
        partyId,
        organizationType,
        organizationName,
        participantId,
        assignedRegions,
      })
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
}
