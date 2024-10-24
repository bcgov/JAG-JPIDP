namespace Pidp.Infrastructure.HttpClients.Jum;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Models.JUSTIN;
using CommonModels.Models.JUSTIN;
using Pidp.Models;
using Prometheus;

public class JumClient : BaseClient, IJumClient
{

    private static readonly Counter InvalidNameMatchCount = Metrics
    .CreateCounter("justin_name_mismatch_total", "Number of name matches resulting in errors.");

    public JumClient(HttpClient httpClient, ILogger<JumClient> logger) : base(httpClient, logger) { }


    /// <summary>
    /// Get a JUSTIN Case - called if Case is not found in EDT
    /// </summary>
    /// <param name="caseId"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<CaseStatusWrapper> GetJustinCaseStatus(string partyId, string caseId, string accessToken)
    {
        var result = await this.GetAsync<CaseStatusWrapper>($"justin-case/{WebUtility.UrlEncode(caseId)}", accessToken);

        if (!result.IsSuccess)
        {
            Serilog.Log.Information($"Failed to get case from JUSTIN {caseId} {result}");

        }
        return result.Value;
    }


    public async Task<Participant?> GetJumUserAsync(string username, string accessToken)
    {
        var result = await this.GetAsync<Participant>($"participant?username={username}", accessToken);

        if (!result.IsSuccess)
        {
            return null;
        }
        var user = result.Value;
        if (user == null)
        {
            this.Logger.LogNoUserFound(username);
            return null;
        }
        if (user.participantDetails.Count == 0)
        {
            this.Logger.LogDisabledUserFound(username);
            return null;
        }
        return user;
    }

    public async Task<Participant?> GetJumUserAsync(string username)
    {


        var result = await this.GetAsync<Participant>($"users/username/{username}");

        if (!result.IsSuccess)
        {
            return null;
        }
        var user = result.Value;
        if (user == null)
        {
            this.Logger.LogNoUserFound(username);
            return null;
        }

        return user;
    }
    public async Task<Participant?> GetJumUserByPartIdAsync(string partId)
    {
        try
        {
            return await this.GetJumUserByPartIdAsync(decimal.Parse(partId));
        }
        catch (FormatException fe)
        {
            Serilog.Log.Error($"Invalid part Id provided to GetJumUserByPartIdAsync({partId})");
            throw fe;
        }

    }

    public async Task<Participant?> GetParticipantByUserNameAsync(string username)
    {
        var result = await this.GetAsync<Participant>($"participant?username={username}");

        if (!result.IsSuccess)
        {
            return null;
        }
        var user = result.Value;
        if (user == null)
        {
            this.Logger.LogNoUserFound(username);
            return null;
        }

        return user;
    }

    public async Task<Participant?> GetJumUserByPartIdAsync(decimal partId)
    {
        var result = await this.GetAsync<Participant>($"participant/{partId}");

        if (!result.IsSuccess)
        {
            return null;
        }
        var user = result.Value;
        if (user == null)
        {
            this.Logger.LogNoUserWithPartIdFound(partId);
            return null;
        }

        return user;
    }
    public async Task<Participant?> GetJumUserByPartIdAsync(decimal partId, string accessToken)
    {
        var result = await this.GetAsync<Participant>($"participant/{partId}", accessToken);

        if (!result.IsSuccess)
        {
            return null;
        }
        var participant = result.Value;
        if (participant.participantDetails.Count == 0)
        {
            this.Logger.LogDisabledPartyIdFound(partId);
            return null;
        }
        if (participant.participantDetails.Count > 1)
        {
            this.Logger.LogMatchMultipleRecord(partId);
            return null;
        }
        if (participant.participantDetails[0].assignedAgencies.Count == 0)
        {
            this.Logger.LogDisabledPartyIdFound(partId);
            return null;
        }
        return participant;
    }

    public Task<bool> IsJumUser(Participant? justinUser, Party party)
    {
        if (justinUser == null || justinUser?.participantDetails.Count == 0
            || party == null)
        {
            this.Logger.LogJustinUserNotFound();
            return Task.FromResult(false);
        }

        if (justinUser!.participantDetails!.FirstOrDefault()!.GrantedRoles.Any(n => n.role.Contains("JRS")))
        {
            //bcps user
            if (justinUser?.participantDetails?.FirstOrDefault()?.firstGivenNm == party.FirstName
                    && justinUser?.participantDetails?.FirstOrDefault()?.surname == party.LastName)
            {
                if (Environment.GetEnvironmentVariable("JUSTIN_SKIP_USER_EMAIL_CHECK") is not null and "true")
                {
                    Serilog.Log.Logger.Warning("JUSTIN EMail address checking is disabled - not checking for {0}", party.Id);
                    return Task.FromResult(true);
                }
                else
                {
                    var partDetails = justinUser?.participantDetails.FirstOrDefault();
                    var upn = partDetails.partUpnTxt;
                    if (string.IsNullOrEmpty(upn))
                    {
                        this.Logger.LogMissingUPN(partDetails.partId);
                        return Task.FromResult(false);

                    }
                    else
                    {
                        if (upn.Equals(party.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            Serilog.Log.Logger.Information("JUSTIN EMail address does not match {0} != {1}", justinUser?.participantDetails?.FirstOrDefault()?.emailAddress, party.Email);
                            return Task.FromResult(false);
                        }
                    }
                }

            }
        }

        if (this.ValidateJUSTINName(justinUser?.participantDetails?.FirstOrDefault(), party))
        {
            return Task.FromResult(true);
        }

        this.Logger.LogJustinUserNotMatching(JsonSerializer.Serialize(justinUser), JsonSerializer.Serialize(party));
        return Task.FromResult(false);
    }

    /// <summary>
    /// Validate First Name, Last Name and EMail address matches between JUSTIN and the users login credentials
    /// </summary>
    /// <param name="justinUser"></param>
    /// <param name="party"></param>
    /// <returns></returns>
    private bool ValidateJUSTINName(ParticipantDetail justinUser, Party party)
    {
        var response = true;
        if (justinUser == null || party == null)
        {
            Serilog.Log.Error("JUSTIN User or party was null in call to ValidateJUSTINName()");
            InvalidNameMatchCount.Inc();
            return false;
        }

        #region last name check
        // check last name
        if (string.IsNullOrEmpty(justinUser.surname))
        {
            Serilog.Log.Error($"JUSTIN surname was empty for user {justinUser.partId}");
            response = false;
        }
        if (string.IsNullOrEmpty(party.LastName))
        {
            Serilog.Log.Error($"Party surname was empty for user {party.Id}");
            response = false;
        }
        if (!string.IsNullOrEmpty(justinUser.surname) && !string.IsNullOrEmpty(party.LastName) && !justinUser.surname.Equals(party.LastName, StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Error($"JUSTIN [{justinUser.surname}] and party surname [{party.LastName}] do not match");
            response = false;
        }
        #endregion

        #region first name check
        // check first name
        if (string.IsNullOrEmpty(justinUser.firstGivenNm))
        {
            Serilog.Log.Error($"JUSTIN first name was empty for user {justinUser.firstGivenNm}");
            response = false;
        }
        if (string.IsNullOrEmpty(party.FirstName))
        {
            Serilog.Log.Error($"Party first name was empty for user {party.Id}");
            response = false;
        }
        if (!string.IsNullOrEmpty(justinUser.firstGivenNm) && !string.IsNullOrEmpty(party.FirstName) && !justinUser.firstGivenNm.Equals(party.FirstName, StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Error($"JUSTIN [{justinUser.firstGivenNm}] and party first name [{party.FirstName}] do not match");
            response = false;
        }
        #endregion

        #region email check
        if (Environment.GetEnvironmentVariable("JUSTIN_SKIP_USER_EMAIL_CHECK") is not null and "true")
        {
            if (string.IsNullOrEmpty(justinUser.partUpnTxt))
            {
                Serilog.Log.Warning($"JUSTIN email was empty for user {justinUser.partId}");
            }
            if (!string.IsNullOrEmpty(justinUser.partUpnTxt) && !string.IsNullOrEmpty(party.Email) && !justinUser.partUpnTxt.Equals(party.Email, StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Warning($"JUSTIN [{justinUser.partUpnTxt}] and party email [{party.Email}] do not match");
            }
        }
        else
        {
            // check email
            if (string.IsNullOrEmpty(justinUser.partUpnTxt))
            {
                Serilog.Log.Error($"JUSTIN email was empty for user {justinUser.partId}");
                response = false;
            }

            if (string.IsNullOrEmpty(party.Email))
            {
                Serilog.Log.Error($"Party email was empty for user {party.Id}");
                response = false;
            }
            if (!string.IsNullOrEmpty(justinUser.partUpnTxt) && !string.IsNullOrEmpty(party.Email) && !justinUser.partUpnTxt.Equals(party.Email, StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Error($"JUSTIN [{justinUser.partUpnTxt}] and party email [{party.Email}] do not match");
                response = false;
            }
        }
        #endregion

        if (response != true)
        {
            InvalidNameMatchCount.Inc();
        }
        // all matches ok
        return response;
    }

    public async Task<bool> FlagUserUpdateAsComplete(int eventMessageId, bool isSuccessful)
    {
        var statusResponse = new JustinProcessStatusModel
        {
            EventMessageId = eventMessageId,
            IsSuccess = isSuccessful
        };

        var result = await this.PutAsync($"user-change-management", statusResponse);
        if (result.IsSuccess)
        {
            Serilog.Log.Information($"Updated JUM User change {statusResponse.ToString()}");
            return true;
        }
        else
        {
            this.Logger.LogFailedToMarkProcessComplete(eventMessageId);
            return false;
        }

    }


}
public static partial class JumClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Provided User information does not match = {expected} {provided}.")]
    public static partial void LogJustinUserNotMatching(this ILogger logger, string expected, string provided);
    [LoggerMessage(2, LogLevel.Warning, "No User found in JUM with Username = {username}.")]
    public static partial void LogNoUserFound(this ILogger logger, string username);
    [LoggerMessage(3, LogLevel.Warning, "No User found in JUM with PartId = {partId}.")]
    public static partial void LogNoUserWithPartIdFound(this ILogger logger, decimal partId);
    [LoggerMessage(4, LogLevel.Warning, "User found but disabled in JUM with Username = {username}.")]
    public static partial void LogDisabledUserFound(this ILogger logger, string username);
    [LoggerMessage(5, LogLevel.Warning, "User found but disabled in JUM with PartId = {partId}.")]
    public static partial void LogDisabledPartIdFound(this ILogger logger, decimal partId);
    [LoggerMessage(6, LogLevel.Error, "Justin user not found.")]
    public static partial void LogJustinUserNotFound(this ILogger logger);
    [LoggerMessage(7, LogLevel.Warning, "User found but disabled in JUM with PartId = {partId}.")]
    public static partial void LogDisabledPartyIdFound(this ILogger logger, decimal partId);
    [LoggerMessage(8, LogLevel.Warning, "Multiple User Records Found with PartId = {partId}.")]
    public static partial void LogMatchMultipleRecord(this ILogger logger, decimal partId);
    [LoggerMessage(9, LogLevel.Error, "Failed to mark event as complete EventMessageId = {eventMessageId}.")]
    public static partial void LogFailedToMarkProcessComplete(this ILogger logger, decimal eventMessageId);
    [LoggerMessage(10, LogLevel.Information, "JUSTIN change Event flagged as complete {eventMessageId}.")]
    public static partial void LogMarkedProcessComplete(this ILogger logger, decimal eventMessageId);
    [LoggerMessage(11, LogLevel.Error, "No UPN for user {user}")]
    public static partial void LogMissingUPN(this ILogger logger, string user);
}
