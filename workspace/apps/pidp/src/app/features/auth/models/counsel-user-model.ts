import { IdentityProvider } from '../enums/identity-provider.enum';
import { UserIdentity } from './user-identity.model';
import { IUserResolver, User } from './user.model';

export class CounselUser implements User {
  public readonly identityProvider: IdentityProvider;
  public ppid: string;
  public userId: string;
  public firstName: string;
  public lastName: string;
  public idir: string;
  public email: string;
  //public roles: string[];

  public constructor({ accessTokenParsed, brokerProfile }: UserIdentity) {
    const { firstName, lastName, email, username: jpdid } = brokerProfile;
    const {
      identity_provider,
      sub: userId,
      preferred_username: idir,
    } = accessTokenParsed;

    this.identityProvider = identity_provider;
    this.ppid = jpdid || '';
    this.userId = userId;
    this.firstName = firstName || '';
    this.lastName = lastName || '';
    this.idir = idir;
    this.email = email || '';
    //this.roles = roles;
  }
}

export class CounselResolver implements IUserResolver<CounselUser> {
  public constructor(public userIdentity: UserIdentity) {}
  public resolve(): CounselUser {
    return new CounselUser(this.userIdentity);
  }
}
