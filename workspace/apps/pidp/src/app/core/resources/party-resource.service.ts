import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable, catchError, exhaustMap, map, of } from 'rxjs';

import { Party, PartyCreate } from '@bcgov/shared/data-access';

import { BcscUser } from '@app/features/auth/models/bcsc-user.model';

import { AuthorizedUserService } from '../services/authorized-user.service';
import { ApiResource } from './api-resource.service';
import { ResourceUtilsService } from './resource-utils.service';

@Injectable({
  providedIn: 'root',
})
export class PartyResource {
  public constructor(
    private apiResource: ApiResource,
    private resourceUtilsService: ResourceUtilsService,
    private authorizedUserService: AuthorizedUserService
  ) {}

  /**
   * @description
   * Discovery endpoint for checking the existence of a Party
   * based on a UserId, and provides a PartyId in response.
   */
  // TODO simplify this discovery endpoint path and respond with ID or null
  public getParties(userId: string): Observable<number | null> {
    const params = this.resourceUtilsService.makeHttpParams({ userId });
    return this.apiResource.get<{ id: number }[]>('parties', { params }).pipe(
      map((parties: { id: number }[]) => {
        let partyId: number | null = null;
        if (parties?.length) {
          partyId = parties.shift()?.id ?? null;
        }
        return partyId;
      }),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 400) {
          return of(null);
        }

        throw error;
      })
    );
  }

  public createParty(party: PartyCreate): Observable<number | null> {
    return this.apiResource.post<number>('parties', party).pipe(
      catchError((error: HttpErrorResponse) => {
        throw error;
      })
    );
  }

  public getParty(partyId: number): Observable<Party | null> {
    return this.apiResource.get<Party>(`parties/${partyId}`).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === HttpStatusCode.NotFound) {
          return of(null);
        }

        throw error;
      })
    );
  }

  public firstOrCreate(): Observable<Party | null> {
    return this.authorizedUserService.user$.pipe(
      exhaustMap((user: BcscUser | null) =>
        user
          ? this.getParties(user.userId).pipe(
              map((partyId: number | null) => partyId ?? user)
            )
          : of(null)
      ),
      exhaustMap((partyIdOrUser: number | BcscUser | null) =>
        !partyIdOrUser || typeof partyIdOrUser === 'number'
          ? of(partyIdOrUser)
          : this.createParty(partyIdOrUser)
      ),
      exhaustMap((partyId: number | null) =>
        partyId ? this.getParty(partyId) : of(null)
      )
    );
  }
}
