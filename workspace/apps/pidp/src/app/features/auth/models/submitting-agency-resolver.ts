import { IdentityProvider } from '../enums/identity-provider.enum';
import { UserIdentity } from './user-identity.model';
import { IUserResolver, User } from './user.model';

export class SubmittingAgencyUser implements User {
  public readonly identityProvider: IdentityProvider;
  public jpdid: string;
  public userId: string;
  public firstName: string;
  public lastName: string;
  public email: string;
  public idpHint: string;
  public constructor({ accessTokenParsed, brokerProfile }: UserIdentity) {
    const { firstName, lastName, email, username: jpdid } = brokerProfile;
    const { identity_provider, sub: userId } = accessTokenParsed;

    this.idpHint = identity_provider;
    this.identityProvider = IdentityProvider.SUBMITTING_AGENCY;
    this.jpdid = jpdid || '';
    this.userId = userId;
    this.firstName = firstName || '';
    this.lastName = lastName || '';

    this.email = email || '';
  }
}

export class SubmittingAgencyResolver
  implements IUserResolver<SubmittingAgencyUser>
{
  public constructor(public userIdentity: UserIdentity) {}
  public resolve(): SubmittingAgencyUser {
    return new SubmittingAgencyUser(this.userIdentity);
  }
}
