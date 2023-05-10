import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, of } from 'rxjs';

import { NoContent, NoContentResponse } from '@bcgov/shared/data-access';

import { ApiHttpClient } from '@app/core/resources/api-http-client.service';

import { PartyModel } from '../../party/party.model';

export interface PartyList {
  id: number;
  providerName?: string;
  providerOrganizationCode?: string;
  organizationName?: string;
  digitalEvidenceAccessRequest?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AdminResource {
  public constructor(private apiResource: ApiHttpClient) {}

  public getParties(): Observable<PartyList[]> {
    return this.apiResource.get<PartyList[]>('/admin/parties').pipe(
      catchError((_: HttpErrorResponse) => {
        // TODO add logging and toast messaging around specific errors when the admin starts getting a bit of attention
        return of([]);
      })
    );
  }

  public getUserDetails(partyId: string): Observable<PartyModel> {
    return this.apiResource.get(`admin/party/${partyId}`, {});
  }

  public resetAccessRequest(partyId: string): Observable<PartyModel> {
    return this.apiResource.put(`admin/party/${partyId}/reset`, {});
  }

  public deleteParties(): NoContent {
    return this.apiResource.delete<PartyList[]>('/admin/parties').pipe(
      NoContentResponse,
      catchError((error: HttpErrorResponse) => {
        // TODO add logging and toast messaging around specific errors when the admin starts getting a bit of attention
        throw error;
      })
    );
  }
}
