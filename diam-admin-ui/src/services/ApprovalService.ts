import KeyCloakService from "@/security/KeycloakService";
import { ApprovalsApi, Configuration } from "../generated/openapi/index";

export class ApprovalService 
{

    configuration = new Configuration({
      basePath: import.meta.env.VITE_APPROVAL_URL,
      accessToken: KeyCloakService.GetToken(),
      headers: { Authorization: "bearer " + KeyCloakService.GetToken() },
    });

    approvalApi = new ApprovalsApi(this.configuration);

}