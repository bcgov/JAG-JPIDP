import { Injectable } from '@angular/core';

import { Observable, combineLatest, filter, map } from 'rxjs';

import { LookupService } from '@app/modules/lookup/lookup.service';

import { IdentityProvider } from '../enums/identity-provider.enum';
import { BcpsResolver } from '../models/bcps-user.model';
import { BcscResolver } from '../models/bcsc-user.model';
import { CounselResolver } from '../models/counsel-user-model';
import { IdirResolver } from '../models/idir-user.model';
import { PhsaResolver } from '../models/phsa-user.model';
import { SubmittingAgencyResolver } from '../models/submitting-agency-resolver';
import { UserIdentity } from '../models/user-identity.model';
import { IUserResolver, User } from '../models/user.model';
import { AccessTokenService } from './access-token.service';

@Injectable({
  providedIn: 'root',
})
export class AuthorizedUserService {
  public constructor(
    private accessTokenService: AccessTokenService,
    private lookupService: LookupService
  ) { }

  /**
   * @description
   * Get the user roles from the access token.
   */
  public get roles(): string[] {
    return this.accessTokenService.roles();
  }

  /**
   * @description
   * Get the authenticated user subtype based on
   * identity provider.
   */
  public get user$(): Observable<User> {
    return this.getUserResolver$().pipe(map((resolver) => resolver.resolve()));
  }

  /**
   * @description
   * Get the identity provider used to authenticate.
   */
  public get identityProvider$(): Observable<IdentityProvider> {
    return this.user$.pipe(map((user: User) => user.identityProvider));
  }

  /**
   * @description
   * Get the authorized user resolver mapped from
   * the access token.
   */
  private getUserResolver$(): Observable<IUserResolver<User>> {
    return combineLatest({
      accessTokenParsed: this.accessTokenService.decodeToken(),
      brokerProfile: this.accessTokenService.loadBrokerProfile(),
    }).pipe(
      map((userIdentity: UserIdentity) => this.getUserResolver(userIdentity))
    );
  }

  /**
   * @description
   * Factory for generating an user subtype based
   * on identity provider.
   */
  private getUserResolver(userIdentity: UserIdentity): IUserResolver<User> {
    // see if came from submitting agency
    const submittingAgency = this.lookupService.submittingAgencies.find(
      (agency) =>
        agency.idpHint === userIdentity.accessTokenParsed?.identity_provider
    );

    // user is in a submitting agency IDP
    if (submittingAgency != null) {
      return new SubmittingAgencyResolver(userIdentity);
    }
    // this is so lame!! - this needs to come from server config!!
    // new services should get possible identity providers
    switch (userIdentity.accessTokenParsed?.identity_provider) {
      case IdentityProvider.AZUREAD:
        return new IdirResolver(userIdentity);

      case IdentityProvider.IDIR, IdentityProvider.AZUREIDIR:
        return new IdirResolver(userIdentity);
      case IdentityProvider.BCSC:
        return new BcscResolver(userIdentity);
      case IdentityProvider.PHSA:
        return new PhsaResolver(userIdentity);
      case IdentityProvider.VERIFIED_CREDENTIALS:
        return new CounselResolver(userIdentity);
      case IdentityProvider.BCPS:
        return new BcpsResolver(userIdentity);
      default:
        console.error("Unknown provider %s", userIdentity.accessTokenParsed?.identity_provider);
        throw new Error(
          'Identity provider not [' +
          userIdentity.accessTokenParsed?.identity_provider +
          '] recognized'
        );
    }
  }
}
