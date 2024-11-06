namespace edt.service.Features.Participant;

using System.Diagnostics.Metrics;
using Common.Models.EDT;
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

            logger.LogInformation($"Starting lookup for {participantId}");

            var initialParticpant = await edtClient.GetPerson(participantId);

            if (initialParticpant == null)
            {
                logger.LogWarning($"No participant found for partId {participantId}");
                return null;
            }


            // track initial id
            processedParticipantIds.Add(participantId);

            // see if this user is active
            logger.LogInformation($"Found initial user with key: {participantId} {initialParticpant}");

            // see if this user has merged keys
            var mergedFieldValue = initialParticpant.Fields.FirstOrDefault(f => f.Name.Equals(configuration.ParticipantMergeLookupConfig.MergedParticipantField, StringComparison.OrdinalIgnoreCase));
            if (mergedFieldValue == null || mergedFieldValue.Value == null || string.IsNullOrEmpty(mergedFieldValue.Value.ToString()))
            {
                logger.LogInformation($"No merge info found for partId {participantId}");

                if (!initialParticpant.IsActive)
                {
                    logger.LogWarning($"Participant is inactive for {participantId} and no merges found");
                }
                return new ParticipantMergeListingModel()
                {
                    PrimaryParticipant = PopulateParticipantFromPerson(initialParticpant)
                };

            }
            else
            {

                var returnParticipant = new ParticipantMergeListingModel()
                {
                    PrimaryParticipant = PopulateParticipantFromPerson(initialParticpant)
                };

                // recurse through all merged participants
                var mergedIdListing = mergedFieldValue.Value.ToString();


                foreach (var mergedId in mergedIdListing.Split(configuration.ParticipantMergeLookupConfig.MergedParticipantDelimiter))
                {
                    // dont process same id more than once
                    if (processedParticipantIds.Contains(mergedId))
                    {
                        logger.LogWarning($"Merged ID {mergedId} already handled for {participantId}");
                        continue;
                    }


                    logger.LogInformation($"Searching merged participant from source {participantId} [{mergedId}]");
                    var mergedParticipant = await edtClient.GetPerson(mergedId);

                    if (mergedParticipant != null)
                    {
                        logger.LogInformation($"Found merged user with key: {mergedId} {mergedParticipant} for initial participant {participantId}");

                        returnParticipant.SourceParticipants.Add(PopulateParticipantFromPerson(mergedParticipant));
                    }
                    else
                    {
                        logger.LogWarning($"Unknown merged participant {mergedId} for participant {participantId}");
                    }
                    processedParticipantIds.Add(mergedId);

                }


                return returnParticipant;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Participant searching failed for {participantId} [{ex.Message}]");
            throw;
        }

    }

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
