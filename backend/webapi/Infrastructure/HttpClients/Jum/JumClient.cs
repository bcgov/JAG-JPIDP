namespace Pidp.Infrastructure.HttpClients.Jum;

using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Azure.Core;
using NodaTime;
using Pidp.Models;

public class JumClient : BaseClient, IJumClient
{
    public JumClient(HttpClient httpClient, ILogger<JumClient> logger) : base(httpClient, logger) { }

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


        var result = await this.GetAsync<Participant>($"users/{username}");

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
                    if (justinUser?.participantDetails?.FirstOrDefault()?.emailAddress.ToUpper(CultureInfo.CurrentCulture) == party.Email!.ToUpper(CultureInfo.CurrentCulture))
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



        if (justinUser?.participantDetails?.FirstOrDefault()?.firstGivenNm == party.FirstName
            && justinUser?.participantDetails?.FirstOrDefault()?.surname == party.LastName
            && justinUser?.participantDetails?.FirstOrDefault()?.emailAddress == party.Email
            //&& !justinUser.IsDisabled
            //&& LocalDate.FromDateTime(justinUser.person.BirthDate) == party.Birthdate
            && LocalDate.FromDateTime(Convert.ToDateTime(justinUser?.participantDetails?.FirstOrDefault()?.birthDate, CultureInfo.CurrentCulture)) == party.Birthdate)
        //&& justinUser?.participantDetails?.FirstOrDefault()?.Gender == party.Gender)
        {
            return Task.FromResult(true);
        }

        this.Logger.LogJustinUserNotMatching(JsonSerializer.Serialize(justinUser), JsonSerializer.Serialize(party));
        return Task.FromResult(false);
    }

    public async Task<bool> FlagUserUpdateAsComplete(int eventMessageId, bool isSuccessful)
    {
        var statusResponse = new JustinProcessStatusModel
        {
            EventMessageId = eventMessageId,
            IsSuccess = isSuccessful
        };

        var result = await this.PostAsync<bool>($"user-change-management", statusResponse);
        if (result.IsSuccess)
        {

        }
        else
        { 
            this.Logger.LogFailedToMarkProcessComplete(eventMessageId);
        }

        return result.Value;
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
}
