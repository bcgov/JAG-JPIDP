import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, throwError } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';
import { ProfileStatus } from '@app/features/portal/models/profile-status.model';
import { PortalResource } from '@app/features/portal/portal-resource.service';

import { DigitalEvidenceCaseAutocompleteResponse } from './digital-evidence-case-autocomplete-response.model';

@Injectable({
  providedIn: 'root',
})
export class DigitalEvidenceCaseManagementResource {
  public constructor(
    private apiResource: ApiHttpClient,
    private portalResource: PortalResource
  ) {}

  public getProfileStatus(partyId: number): Observable<ProfileStatus | null> {
    return this.portalResource.getProfileStatus(partyId);
  }

  public searchCases(
    searchTerm: string
  ): Observable<DigitalEvidenceCaseAutocompleteResponse[]> {
    return this.apiResource.get(
      `access-requests/digital-evidence-case-search?search=${searchTerm}`
    );
  }

  public requestAccess(partyId: number): NoContent {
    return this.apiResource
      .post<NoContent>('access-requests/digital-evidence-case-management', {
        partyId,
      })
      .pipe(
        NoContentResponse,
        catchError((error: HttpErrorResponse) => {
          return throwError(() => error);
        })
      );
  }
}
