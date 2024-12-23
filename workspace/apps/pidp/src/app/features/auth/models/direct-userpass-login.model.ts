import { IdentityProvider } from '../enums/identity-provider.enum';
import { UserIdentity } from './user-identity.model';
import { IUserResolver, User } from './user.model';

export class UserPassUser implements User {
  public readonly identityProvider: IdentityProvider;
  public jpdid: string;
  public userId: string;
  public firstName: string;
  public lastName: string;
  public idir: string;
  public email: string;

  public constructor({ accessTokenParsed, brokerProfile }: UserIdentity) {
    const { firstName, lastName, email, username: jpdid } = brokerProfile;
    const {
      identity_provider,
      preferred_username: idir,
      sub: userId,
    } = accessTokenParsed;

    this.identityProvider = identity_provider;
    this.idir = idir;
    this.userId = userId;
    this.jpdid = jpdid || '';
    this.firstName = firstName || '';
    this.lastName = lastName || '';
    this.email = email || '';
  }
}

export class UserPassResolver implements IUserResolver<UserPassUser> {
  public constructor(public userIdentity: UserIdentity) {}
  public resolve(): UserPassUser {
    return new UserPassUser(this.userIdentity);
  }
}
