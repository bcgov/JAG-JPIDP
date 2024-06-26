namespace MessagingAdapter.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MessagingAdapter.Models;
using MessagingAdapter.ServicesHttpClients;
using Microsoft.Extensions.Logging;
using Prometheus;

public class EdtCoreClient : BaseClient, IEdtCoreClient
{
    private readonly IConfiguration configuration;
    private static readonly Counter EventPollCount = Metrics
        .CreateCounter("disclosure_event_polls_total", "Number of disclosure event polls.");

    public EdtCoreClient(
        HttpClient httpClient, IConfiguration configuration,
        ILogger<EdtCoreClient> logger)
        : base(httpClient, logger)
    {

        this.configuration = configuration;
    }

    /// <summary>
    /// GET api/v1/platform-events?filter=CreatedUtc:>=2024-05-05T12:34:56Z,Id:>222 
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns></returns>
    public async Task<List<DisclosureEventModel>> GetDisclosureEvents(DateTime fromDate)
    {

        var returnEvents = new List<DisclosureEventModel>();
        var lastPollEvent = fromDate.ToString("o");

        var result = await this.GetAsync<List<DisclosureEventModel>>($"api/v1/platform-events?filter=CreatedUtc:>{lastPollEvent}");

        if (result.IsSuccess)
        {
            returnEvents = result.Value;
        }


        return returnEvents;
    }
}
