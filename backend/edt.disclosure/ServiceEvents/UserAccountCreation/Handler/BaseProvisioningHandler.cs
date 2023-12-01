namespace edt.disclosure.ServiceEvents.UserAccountCreation.Handler;

using Common.Models.EDT;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Model;

public abstract class BaseProvisioningHandler
{
    protected IEdtDisclosureClient edtClient { get; set; }
    protected readonly EdtDisclosureServiceConfiguration configuration;

    protected BaseProvisioningHandler(IEdtDisclosureClient edtClient, EdtDisclosureServiceConfiguration configuration)
    {
        this.edtClient = edtClient;
        this.configuration = configuration;
    }

    protected async Task<string> CheckEdtServiceVersion() => await this.edtClient.GetVersion();

    protected async Task<UserModificationEvent> AddOrUpdateUser(EdtDisclosureUserProvisioningModel value)
    {
        var user = await this.edtClient.GetUser(value.Key!);


        //create user account in EDT
        var result = user == null
            ? await this.edtClient.CreateUser(value)
            : await this.edtClient.UpdateUser(value, user);
        return result;



    }

    protected async Task<CaseModel> CreateUserFolio(EdtDisclosureUserProvisioningModel accessRequestModel)
    {
        // check case isnt present - key changes depending on user type
        var caseKey = (accessRequestModel is EdtDisclosurePublicUserProvisioningModel) ? ((EdtDisclosurePublicUserProvisioningModel)accessRequestModel).PersonKey : accessRequestModel.Key;

        var caseModel = await this.edtClient.FindCaseByKey(caseKey);
        if (caseModel != null)
        {
            return caseModel;
        }

        var caseName = (accessRequestModel.OrganizationType == this.configuration.EdtClient.OutOfCustodyOrgType)
            ? accessRequestModel.FullName + "(" + caseKey + " Accused Folio)"
            : accessRequestModel.FullName + "(" + caseKey + " Defence Folio)";

        var caseCreation = (accessRequestModel is EdtDisclosurePublicUserProvisioningModel) ?
            new EdtCaseDto
            {
                Name = caseName,
                Description = "Folio Case for Accused",
                Key = caseKey,
                //  TemplateCase = (this.configuration.EdtClient.OutOfCustodyTemplateId < 0 && !string.IsNullOrEmpty(this.configuration.EdtClient.OutOfCustodyTemplateName)) ? this.configuration.EdtClient.OutOfCustodyTemplateName : null,
                TemplateCase = (this.configuration.EdtClient.OutOfCustodyTemplateId > -1) ? "" + this.configuration.EdtClient.OutOfCustodyTemplateId : "",

            } :

            new EdtCaseDto
            {
                Name = caseName,
                Description = "Folio Case for Defence Counsel",
                Key = caseKey,
                //  TemplateCase = (this.configuration.EdtClient.DefenceFolioTemplateId < 0 && !string.IsNullOrEmpty(this.configuration.EdtClient.DefenceFolioTemplateName)) ? this.configuration.EdtClient.DefenceFolioTemplateName : null,
                TemplateCase = (this.configuration.EdtClient.DefenceFolioTemplateId > -1) ? "" + this.configuration.EdtClient.DefenceFolioTemplateId : "",

            };

        Serilog.Log.Information($"Creating new case {caseCreation.Name} Template {caseCreation.TemplateCase}");

        var createResponseID = await this.edtClient.CreateCase(caseCreation);

        caseModel = await this.edtClient.GetCase(createResponseID);


        return caseModel;


    }

    protected async Task<bool> LinkUserToFolio(EdtDisclosureUserProvisioningModel accessRequestModel, int caseId)
    {
        var addGroupCount = 0;

        var user = await this.edtClient.GetUser(accessRequestModel.Key!) ?? throw new EdtDisclosureServiceException($"User was not found {accessRequestModel.Key}");

        var caseGroups = (accessRequestModel.OrganizationType == this.configuration.EdtClient.OutOfCustodyOrgType) ? this.configuration.EdtClient.OutOfCustodyCaseGroups : this.configuration.EdtClient.DefenceCaseGroups;
        var caseGroupArr = caseGroups.Split(",", StringSplitOptions.TrimEntries);

        if (caseGroupArr.Any())
        {
            foreach (var group in caseGroupArr)
            {
                if (await this.edtClient.AddUserToCase(user.Id, caseId, group))
                {
                    addGroupCount++;
                }
                else
                {
                    Serilog.Log.Error($"Failed to add group {group} to case {caseId}");
                }

            }
        }
        else
        {
            Serilog.Log.Warning($"*** NO case group assigned in configuration - no case group will be assigned for user {user.Id} and case {caseId} ***");
            if (await this.edtClient.AddUserToCase(user.Id, caseId))
            {
                addGroupCount++;
            }
            else
            {
                Serilog.Log.Warning($"Failed to add user to case {user.Id} {caseId}");
            }
        }

        if (addGroupCount == 0)
        {
            throw new EdtDisclosureServiceException($"Failed to add user {user.Id} to folio");
        }
        if (caseGroups.Any() && addGroupCount != caseGroupArr.Length)
        {
            throw new EdtDisclosureServiceException($"Failed to add user {user.Id} to all case groups for folio {caseId}");
        }

        Serilog.Log.Information($"Added groups {addGroupCount} to user {user.Id} folio {caseId}");
        return true;
    }

}
