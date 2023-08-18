import KeyCloakService from "@/security/KeycloakService";
import { ApprovalsApi, Configuration } from "../generated/openapi/index";
import { environment } from "@/environments/environment";

export class ApprovalService 
{

    configuration = new Configuration({
      basePath: environment.approvalUrl,
      accessToken: KeyCloakService.GetToken(),
      headers: { Authorization: "bearer " + KeyCloakService.GetToken() },
    });

    approvalApi = new ApprovalsApi(this.configuration);

}