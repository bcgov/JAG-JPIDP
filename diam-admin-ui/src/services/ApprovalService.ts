import KeyCloakService from "@/security/KeycloakService";
import { ApprovalsApi, Configuration, type CommonModelsApprovalApprovalModel } from "../generated/openapi/index";

export class ApprovalService 
{

    configuration = new Configuration({
      basePath: "https://localhost:7231",
      accessToken: KeyCloakService.GetToken(),
      headers: { Authorization: "bearer " + KeyCloakService.GetToken() },
    });



    approvalApi = new ApprovalsApi(this.configuration);

}