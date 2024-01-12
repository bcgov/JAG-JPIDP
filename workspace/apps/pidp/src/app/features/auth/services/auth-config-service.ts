import { Observable } from "rxjs";
import { LoginConfigModel } from "../models/loginConfigModel";
import { ApiConfigHttpClient, ApiHttpClient } from "@app/core/resources/api-http-client.service";
import { Injectable } from "@angular/core";

export interface IAuthConfigService {
  getLoginOptions(): Observable<LoginConfigModel[]>;
}
@Injectable({
  providedIn: 'root',
})
export class AuthConfigService implements IAuthConfigService {

  public constructor(private apiResource: ApiConfigHttpClient, private webApiResource: ApiHttpClient) { }


  public getLoginOptions(): Observable<LoginConfigModel[]> {
    return this.apiResource.get("api/config/user-group/login-options", {});
  }

  public getUserConfigOptions(userType: string): Observable<Map<string, string>> {
    console.log("User %o", userType);
    return this.webApiResource.get("api/configuration/" + userType);

  }


}
