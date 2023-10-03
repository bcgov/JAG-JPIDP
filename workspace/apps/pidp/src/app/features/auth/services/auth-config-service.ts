import { Observable } from "rxjs";
import { LoginConfigModel } from "../models/loginConfigModel";
import { ApiConfigHttpClient } from "@app/core/resources/api-http-client.service";
import { Injectable } from "@angular/core";

export interface IAuthConfigService {
  getLoginOptions(): Observable<LoginConfigModel[]>;
}
@Injectable({
  providedIn: 'root',
})
export class AuthConfigService implements IAuthConfigService {

  public constructor(private apiResource: ApiConfigHttpClient) { }


  public getLoginOptions(): Observable<LoginConfigModel[]> {
    return this.apiResource.get("api/config/user-group/login-options", {});
  }


}
