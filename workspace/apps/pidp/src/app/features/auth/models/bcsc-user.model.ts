import { IdentityProvider } from '../enums/identity-provider.enum';
import { UserIdentity } from './user-identity.model';
import { IUserResolver, User } from './user.model';

export class BcscUser implements User {
  public readonly identityProvider: IdentityProvider;
  public hpdid: string;
  public userId: string;
  public firstName: string;
  public lastName: string;
  public birthdate: string;
  public gender: string;

  public constructor({ accessTokenParsed, brokerProfile }: UserIdentity) {
    const {
      firstName,
      lastName,
      username: hpdid,
      attributes: {
        birthdate: [birthdate],
        gender: [gender]
      },
    } = brokerProfile;
    const { identity_provider, sub: userId } = accessTokenParsed;

    this.identityProvider = identity_provider;
    this.hpdid = hpdid;
    this.userId = userId;
    this.firstName = firstName;
    this.lastName = lastName;
    this.birthdate = birthdate;
    this.gender = gender;
  }
}

export class BcscResolver implements IUserResolver<BcscUser> {
  public constructor(public userIdentity: UserIdentity) {}
  public resolve(): BcscUser {
    return new BcscUser(this.userIdentity);
  }
}
