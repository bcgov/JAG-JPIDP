namespace edt.disclosure.ServiceEvents.UserAccountCreation.Handler;

using Common.Models.EDT;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Kafka.Model;
using Prometheus;

public abstract class BaseProvisioningHandler
{
    protected IEdtDisclosureClient edtClient { get; set; }
    protected readonly EdtDisclosureServiceConfiguration configuration;
    protected readonly IKafkaProducer<string, PersonFolioLinkageModel> folioLinkageProducer;
    protected readonly string edtCoreDisclosureInstanceId;
    private static readonly Histogram UserFolioCreationHistogram = Metrics.CreateHistogram("edt_disclosure_folio_creation", "Histogram of edt disclosure folio creation times.");
    private static readonly Counter FolioLinkageRequests = Metrics
    .CreateCounter("edt_disclosure_folio_linkage_count_total", "Number of disclosure folio linkage requests.");
    private static readonly Counter FolioLinkageRequestFailures = Metrics
.CreateCounter("edt_disclosure_folio_linkage_failure_total", "Number of failed disclosure folio linkage requests.");

    protected BaseProvisioningHandler(IEdtDisclosureClient edtClient, EdtDisclosureServiceConfiguration configuration, IKafkaProducer<string, PersonFolioLinkageModel> producer)
    {
        this.edtClient = edtClient;
        this.configuration = configuration;
        this.folioLinkageProducer = producer;
        this.edtCoreDisclosureInstanceId = this.configuration.EdtCoreDisclosureInstanceId;
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

        using (UserFolioCreationHistogram.NewTimer())
        {
            // check case isnt present - key changes depending on user type
            string? caseKey;
            if (accessRequestModel is EdtDisclosurePublicUserProvisioningModel model)
            {
                caseKey = model.PersonKey;
                Serilog.Log.Information($"Public user {accessRequestModel.UserName} - creating folio with key: {caseKey}");

            }
            else
            {

                caseKey = accessRequestModel.Key;
                Serilog.Log.Information($"Defence user {accessRequestModel.UserName} - creating folio with key: {caseKey}");

            }

            var caseModel = await this.edtClient.FindCaseByKey(caseKey);
            if (caseModel != null)
            {
                return caseModel;
            }

            var caseName = (accessRequestModel is EdtDisclosurePublicUserProvisioningModel)
                ? accessRequestModel.FullName + " (" + caseKey + " Accused Folio)"
                : accessRequestModel.FullName + " (" + caseKey + " Defence Folio)";

            var caseCreation = (accessRequestModel is EdtDisclosurePublicUserProvisioningModel) ?
                new EdtCaseDto
                {
                    Name = caseName,
                    Description = "Folio Case for Accused",
                    Key = caseKey,
                    TemplateCase = (this.configuration.EdtClient.OutOfCustodyTemplateId > -1) ? "" + this.configuration.EdtClient.OutOfCustodyTemplateId : "",
                } :

                new EdtCaseDto
                {
                    Name = caseName,
                    Description = "Folio Case for Defence Counsel",
                    Key = caseKey,
                    TemplateCase = (this.configuration.EdtClient.DefenceFolioTemplateId > -1) ? "" + this.configuration.EdtClient.DefenceFolioTemplateId : "",
                };

            Serilog.Log.Information($"Creating new case {caseCreation.Name} Template {caseCreation.TemplateCase}");

            var createResponseID = await this.edtClient.CreateCase(caseCreation);

            caseModel = await this.edtClient.GetCase(createResponseID);


            return caseModel;

        }
    }

    protected async Task<bool> LinkUserToFolio(EdtDisclosureUserProvisioningModel accessRequestModel, int caseId)
    {
        var addGroupCount = 0;

        var user = await this.edtClient.GetUser(accessRequestModel.Key!) ?? throw new EdtDisclosureServiceException($"User was not found {accessRequestModel.Key}");

        var caseGroups = (accessRequestModel.OrganizationType == this.configuration.EdtClient.OutOfCustodyOrgType) ? this.configuration.EdtClient.OutOfCustodyCaseGroups : this.configuration.EdtClient.DefenceCaseGroups;
        var caseGroupArr = caseGroups.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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

        Serilog.Log.Information($"Added groups {addGroupCount} [{string.Join(",", caseGroupArr)}] to user {user.Id} folio {caseId}");

        // once linkage is complete we need to publish back to core the ID for the folio and the user info
        var msgKey = Guid.NewGuid().ToString();
        var edtKey = (accessRequestModel is EdtDisclosurePublicUserProvisioningModel model) ? model.PersonKey : "";
        var produced = await this.folioLinkageProducer.ProduceAsync(this.configuration.KafkaCluster.CoreFolioCreationNotificationTopic, msgKey, new PersonFolioLinkageModel
        {
            DisclosureCaseIdentifier = edtCoreDisclosureInstanceId + "-" + caseId,
            PersonKey = user.Key,
            EdtExternalId = edtKey,
            AccessRequestId = accessRequestModel.AccessRequestId,
            PersonType = (accessRequestModel is EdtDisclosureDefenceUserProvisioningModel) ? "Counsel" : "Public",
            Status = "Pending"
        });

        if (produced != null && produced.Status == Confluent.Kafka.PersistenceStatus.Persisted)
        {
            Serilog.Log.Information($"{msgKey} persisted for core folio linkage {caseId} for person {user.Key}");
            FolioLinkageRequests.Inc();
            return true;
        }
        else
        {
            Serilog.Log.Error($"Failed to produce to topic {this.configuration.KafkaCluster.CoreFolioCreationNotificationTopic} ({msgKey})");
            FolioLinkageRequestFailures.Inc();
            return false;
        }
    }

}
