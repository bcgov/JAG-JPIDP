namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoMapper;
using DomainResults.Common;

using edt.disclosure.Exceptions;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Serilog;

public class EdtDisclosureClient : BaseClient, IEdtDisclosureClient
{
    private readonly IMapper mapper;
    private readonly Infrastructure.Telemetry.OtelMetrics meters;
    private readonly EdtDisclosureServiceConfiguration configuration;
    private static readonly Counter ProcessedJobCount = Metrics
        .CreateCounter("case_search_count", "Number of case search requests.");


    public EdtDisclosureClient(
        HttpClient httpClient, Infrastructure.Telemetry.OtelMetrics meters, EdtDisclosureServiceConfiguration edtServiceConfiguration,
        IMapper mapper,
        ILogger<EdtDisclosureClient> logger)
        : base(httpClient, logger)
    {
        this.mapper = mapper;
        this.meters = meters;
        this.configuration = edtServiceConfiguration;
    }


    public async Task<EdtUserDto?> GetUser(string userKey)
    {

        this.meters.GetUser();
        Log.Logger.Information("Checking if user key {0} already present", userKey);
        var result = await this.GetAsync<EdtUserDto?>($"api/v1/users/key:{userKey}");

        if (!result.IsSuccess)
        {
            return null;
        }
        return result.Value;
    }




    /// <summary>
    /// Get the current EDT version
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<string> GetVersion()
    {
        var result = await this.GetAsync<EdtVersion?>($"api/v1/version");

        if (!result.IsSuccess)
        {
            throw new EdtDisclosureServiceException(string.Join(",", result.Errors));
        }

        return result.Value.Version;
    }


    public async Task<Task> HandleCourtLocationRequest(string key, CourtLocationDomainEvent accessRequest)
    {
        Log.Debug($"handling court access request {accessRequest.RequestId}");

        // check user is present

        // get the id of the case


        return Task.CompletedTask;
    }

    public async Task<CourtLocationCaseModel> FindLocationCase(string caseName)
    {
        //' /api/v1/org-units/1/cases/3:105: 23-472018/id
        ProcessedJobCount.Inc();
        Log.Logger.Information("Finding case {0}", caseName);

        var caseSearch = await this.GetAsync<IEnumerable<CourtLocationCaseModel>?>($"api/v1/org-units/1/cases/{caseName}/id");

        var cases = caseSearch?.Value;
        int caseId;

        if (cases?.Count() > 1)
        {
            Log.Information("Multiple cases found for {0}", caseName);
            caseId = cases.FirstOrDefault(c => c.Key != null).Id;
        }
        else
        {
            caseId = cases.First().Id;
        }

        var result = await this.GetAsync<CourtLocationCaseModel?>($"api/v1/cases/{caseId}");

        if (result.IsSuccess)
        {
            return result?.Value;
        }
        else
        {
            throw new EdtDisclosureServiceException($"Failed to find case {caseName} : {string.Join(",", result.Errors)}");
        }

    }



    public static class RequestEventType
    {
        public const string Provisioning = "court-case-provision-event";
        public const string Decommission = "court-case-decommission-event";
        public const string None = "None";
    }

    public class CaseGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
