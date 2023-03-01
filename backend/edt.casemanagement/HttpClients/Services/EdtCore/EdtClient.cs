namespace edt.casemanagement.HttpClients.Services.EdtCore;

using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using AutoMapper;
using DomainResults.Common;
using edt.casemanagement.Exceptions;
using edt.casemanagement.Features.Cases;
using edt.casemanagement.Infrastructure.Telemetry;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Serilog;

public class EdtClient : BaseClient, IEdtClient
{
    private readonly IMapper mapper;
    private readonly OtelMetrics meters;
    private readonly EdtServiceConfiguration configuration;


    public EdtClient(
        HttpClient httpClient, OtelMetrics meters, EdtServiceConfiguration edtServiceConfiguration,
        IMapper mapper,
        ILogger<EdtClient> logger)
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

    public async Task<int> GetOuGroupId(string regionName)
    {
        IDomainResult<IEnumerable<OrgUnitModel?>>? result = await this.GetAsync<IEnumerable<OrgUnitModel?>>($"api/v1/org-units/1/groups");

        if (!result.IsSuccess)
        {
            return 0; //invalid
        }

        return result.Value!
            .Where(ou => ou!.Name == regionName)
            .Select(ou => ou!.Id)
            .FirstOrDefault();
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
            throw new EdtServiceException(string.Join(",", result.Errors));
        }

        return result.Value.Version;
    }




    public async Task<CaseModel> FindCase(string caseIdOrKey)
    {
        //' /api/v1/org-units/1/cases/3:105: 23-472018/id

        var searchString = this.configuration.EdtClient.SearchFieldId + ":" + caseIdOrKey;
        Log.Logger.Information("Finding case {0}", searchString);

        var caseSearch = await this.GetAsync<IEnumerable<CaseLookupModel>?>($"api/v1/org-units/1/cases/{searchString}/id");

        if (caseSearch.IsSuccess)
        {
            var caseSearchValue = caseSearch?.Value;

            // Do something with the caseSearchValue
            if (caseSearch.IsSuccess)
            {
                // see if we have multiple cases with the same id - if e do then ew want the one with a key
                var cases = caseSearch.Value;
                int caseId;

                if (cases.Count() > 1)
                {
                    Log.Information("Multiple cases found for {0}", searchString.ToString());
                    caseId = cases.FirstOrDefault(c => c.Key != null).Id;
                }
                else
                {
                    caseId = cases.First().Id;
                }

                var result = await this.GetAsync<CaseModel?>($"api/v1/cases/{caseId}");

                if (result.IsSuccess)
                {
                    if (result != null)
                    {
                        // filter out unwanted data
                        var customFieldsArray = this.configuration.CaseDisplayCustomFields.Select(f => f.Id).ToArray();
                        var customFieldsIds = this.configuration.CaseDisplayCustomFields.Select(f => f.Id).ToList();
                        var filteredFields = result.Value.Fields.Where(f => customFieldsIds.Contains(f.Id)).ToList();

                        var removeValues = new List<int>();

                        foreach (var field in filteredFields)
                        {
                            var defn = this.configuration.CaseDisplayCustomFields.FirstOrDefault(f => f.Id == field.Id);
                            if (defn.RelatedId > 0)
                            {
                                var relatedField = result.Value.Fields.FirstOrDefault(f => f.Id == defn.RelatedId);
                                if (relatedField != null)
                                {
                                    if (defn.RelatedValueEmpty)
                                    {
                                        if (relatedField.Value != null && !string.IsNullOrEmpty(relatedField.Value.ToString()))
                                        {
                                            removeValues.Add(field.Id);
                                        }
                                    }
                                    else
                                    {
                                        if (relatedField.Value == null || string.IsNullOrEmpty(relatedField.Value.ToString()))
                                        {
                                            removeValues.Add(field.Id);
                                        }
                                    }
                                }
                            }

                            if(!defn.Display && !removeValues.Contains(field.Id))
                            {
                                //removeValues.Add(field.Id);
                                field.Display = false;
                            }
                        }

                        if (removeValues.Count > 0)
                        {
                            filteredFields = filteredFields.Where(f => !removeValues.Contains(f.Id)).ToList();
                        }


                        filteredFields.Sort((f1, f2) =>
                        {
                            var index1 = Array.IndexOf(customFieldsArray, f1.Id);
                            var index2 = Array.IndexOf(customFieldsArray, f2.Id);
                            return index1.CompareTo(index2);
                        });

                        result.Value.Fields = filteredFields;
                        return result.Value;
                    }
                    else
                    {
                        throw new EdtServiceException("No data found for case query");
                    }

                }
                else
                {
                    throw new EdtServiceException(string.Join(",", result.Errors));
                }

            }
            else
            {
                throw new EdtServiceException(string.Join(",", caseSearch.Errors));
            }
        }
        else
        {
            throw new EdtServiceException(string.Join(",", caseSearch.Errors));
        }

    }

    public async Task<Task> HandleCaseRequest(string userKey, SubAgencyDomainEvent accessRequest)
    {
        // get the edt user

        var edtUser = await this.GetUser(userKey);

        if (edtUser == null)
        {
            throw new EdtServiceException("Invalid Edt user " + userKey);
        }

        if (accessRequest.EventType.Equals("Provisioning", StringComparison.Ordinal))
        {
            Log.Information("Case provision request {0} {1}", userKey, accessRequest.CaseId);


        }
        else if (accessRequest.EventType.Equals("Decommission", StringComparison.Ordinal))
        {
            Log.Information("Case decomission request {0} {1}", userKey, accessRequest.CaseId);
        }
        else
        {
            throw new EdtServiceException("Invalid case request event type " + accessRequest.EventType);
        }

        return Task.CompletedTask;
    }

    public static class CaseEventType
    {
        public const string Provisioning = "Provisioning";
        public const string Decommission = "Decommission";
        public const string None = "None";
    }
}
