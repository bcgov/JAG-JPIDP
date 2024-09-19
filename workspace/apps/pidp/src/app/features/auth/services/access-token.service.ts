import { JsonPipe } from '@angular/common';
import { Injectable } from '@angular/core';

import { Observable, from, map } from 'rxjs';

import { JwtHelperService } from '@auth0/angular-jwt';
import { KeycloakService } from 'keycloak-angular';
import { KeycloakProfile } from 'keycloak-js';

import { AccessTokenParsed } from '../models/access-token-parsed.model';
import { BrokerProfile } from '../models/broker-profile.model';

export interface IAccessTokenService {
  token(): Observable<string>;
  isTokenExpired(): boolean;
  decodeToken(): Observable<AccessTokenParsed>;
  roles(): string[];
  groups(): string[];
  clearToken(): void;
}

@Injectable({
  providedIn: 'root',
})
export class AccessTokenService implements IAccessTokenService {
  private jwtHelper: JwtHelperService;

  public constructor(private keycloakService: KeycloakService) {
    this.jwtHelper = new JwtHelperService();
  }

  public token(): Observable<string> {
    return from(this.keycloakService.getToken());
  }

  public isTokenExpired(): boolean {
    return this.keycloakService.isTokenExpired();
  }

  public decodeToken(): Observable<AccessTokenParsed> {
    return this.token().pipe(
      map((token: string | null) => {
        if (token) {
          return this.jwtHelper.decodeToken(token) as AccessTokenParsed;
        }
        return {} as AccessTokenParsed;
      })
    );
  }

  public loadBrokerProfile(forceReload?: boolean): Observable<BrokerProfile> {
    const keycloakUserFromPromise =
      this.keycloakService.loadUserProfile(forceReload);

    return from(keycloakUserFromPromise).pipe(
      map((keycloakProfile: KeycloakProfile) => {
        const brokerProfile: BrokerProfile = {
          ...keycloakProfile,
          attributes: {
            birthdate: '',
            gender: '',
            member_status: '',
            member_status_code: '',
          },
        };

        // Make any necessary modifications to brokerProfile here

        return brokerProfile;
      })
    );
  }

  public roles(): string[] {
    const roles = this.keycloakService.getUserRoles();
    return roles;
  }

  public groups(): string[] {
    let roles: string[] = [];
    this.keycloakService.loadUserProfile().then(() => {
      roles = this.keycloakService.getUserRoles();
      return roles; //gives you array of all attributes of user, extract what you need
    });
    return roles;
  }

  public clearToken(): void {
    this.keycloakService.clearToken();
  }
}
