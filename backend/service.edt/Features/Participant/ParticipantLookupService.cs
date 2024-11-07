namespace edt.service.Features.Participant;

using System.Diagnostics.Metrics;
using Common.Exceptions;
using Common.Exceptions.EDT;
using Common.Models.EDT;
using CommonConstants.Constants.DIAM;
using CommonModels.Models.Party;
using edt.service.Data;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Infrastructure.Telemetry;

public class ParticipantLookupService(ILogger<ParticipantLookupService> logger,
    EdtServiceConfiguration configuration,
    EdtDataStoreDbContext dbContext,
    IEdtClient edtClient,
    Instrumentation instrumentation) : IParticipantLookupService
{

    private readonly Counter<long> participantMergeSearchCounter = instrumentation.ParticipantMergeCounter;


    /// <summary>
    /// Recursive call to get participants that are merged together
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="knownParticipantIds"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, EdtPersonDto>> GetAssociatedParticipants(string participantId, Dictionary<string, EdtPersonDto> knownParticipantIds)
    {

        // get initial participant
        var participant = await edtClient.GetPerson(participantId);

        if (participant == null)
        {
            throw new RecordNotFoundException("participant", participantId);
        }
        else
        {
            // only accused participants get merged!
            participant.Role = DIAMConstants.ACCUSED;

            knownParticipantIds.Add(participantId, participant);
            var mergedFieldValue = participant.Fields.FirstOrDefault(f => f.Name.Equals(configuration.ParticipantMergeLookupConfig.MergedParticipantField, StringComparison.OrdinalIgnoreCase));
            if (mergedFieldValue == null || mergedFieldValue.Value == null || string.IsNullOrEmpty(mergedFieldValue.Value.ToString()))
            {
                if (!participant.IsActive)
                {
                    logger.LogWarning($"No part merge info found for {participant} that is inactive");
                }
            }
            else
            {
                var mergedIdListing = mergedFieldValue.Value.ToString();
                foreach (var mergedId in mergedIdListing.Split(configuration.ParticipantMergeLookupConfig.MergedParticipantDelimiter))
                {
                    if (knownParticipantIds.ContainsKey(mergedId))
                    {
                        continue;
                    }

                    await this.GetAssociatedParticipants(mergedId, knownParticipantIds);
                }
            }





        }

        return knownParticipantIds;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="additionalFieldLookups"></param>
    /// <returns></returns>
    public async Task<ParticipantMergeListingModel> GetParticipantMergeDetails(string participantId)
    {
        try
        {
            this.participantMergeSearchCounter.Add(1);
            List<string> processedParticipantIds = [];
            Dictionary<string, EdtPersonDto> participantMap = [];
            logger.LogInformation($"Starting lookup for {participantId}");
            ParticipantMergeListingModel responseModel = new ParticipantMergeListingModel();

            // get all associated participants
            var associatedParticipants = await this.GetAssociatedParticipants(participantId, participantMap);

            // find active participant
            var activeParticpant = associatedParticipants.Values.Where(participant => participant.IsActive).Select(this.PopulateParticipantFromPerson).FirstOrDefault();
            if (activeParticpant != null)
            {
                responseModel.PrimaryParticipant = activeParticpant;

                responseModel.SourceParticipants = associatedParticipants.Values.Where(participant => !participant.IsActive).Select(this.PopulateParticipantFromPerson).ToList();
            }
            else
            {
                logger.LogWarning($"No participants associated to {participantId} are active");
                throw new ParticipantLookupException($"No participants associated to {participantId} are active");

            }


            return responseModel;
        }
        catch (Exception ex)
        {
            logger.LogError($"Participant searching failed for {participantId} [{ex.Message}]");
            throw;
        }

    }


    /// <summary>
    /// Create a person model from the EdtPersonDTO
    /// </summary>
    /// <param name="person"></param>
    /// <returns></returns>
    private ParticipantMergeModel PopulateParticipantFromPerson(EdtPersonDto person)
    {
        var dateOfBirthField = person.Fields.FirstOrDefault(f => f.Name.Equals(configuration.ParticipantMergeLookupConfig.DateOfBirthField, StringComparison.OrdinalIgnoreCase));
        DateOnly dob = DateOnly.MinValue;
        if (dateOfBirthField != null && dateOfBirthField.Value != null && !string.IsNullOrEmpty(dateOfBirthField.Value.ToString()))
        {
            DateOnly.TryParseExact(dateOfBirthField.Value.ToString(), configuration.ParticipantMergeLookupConfig.DateOfBirthFormat, out var dateOfBirth);
            dob = dateOfBirth;
        }

        Dictionary<string, string> additionalFieldValues = [];

        foreach (var lookupField in configuration.ParticipantMergeLookupConfig.AdditionalLookupFields)
        {
            var fieldValue = person.Fields.FirstOrDefault(f => f.Name.Equals(lookupField, StringComparison.OrdinalIgnoreCase));
            if (fieldValue != null && fieldValue.Value != null)
            {
                additionalFieldValues.Add(fieldValue.Name, fieldValue.Value.ToString());
            }
        }

        foreach (var identifier in configuration.ParticipantMergeLookupConfig.Identifiers)
        {
            var identifierField = person.Identifiers.FirstOrDefault(ident => ident.IdentifierType.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (identifierField != null && identifierField.IdentifierValue != null)
            {
                additionalFieldValues.Add(identifierField.IdentifierType, identifierField.IdentifierValue.ToString());
            }
        }

        return new ParticipantMergeModel()
        {
            FirstName = person.FirstName,
            LastName = person.LastName,
            ParticipantId = person.Key,
            EdtID = person.Id,
            IsActive = person.IsActive,
            DateOfBirth = dob,
            ParticipantInfo = additionalFieldValues
        };
    }
}
