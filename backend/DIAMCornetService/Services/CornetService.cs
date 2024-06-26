namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using Common.Kafka;
using Confluent.Kafka;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;

public class CornetService(ILogger<NotificationService> logger, IKafkaProducer<string, ParticipantCSNumberModel> producer, DIAMCornetServiceConfiguration cornetServiceConfiguration, ICornetORDSClient cornetORDSClient) : ICornetService
{

    /// <summary>
    /// To be implemented fully JAMAL
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public async Task<ParticipantCSNumberModel> GetParticipantCSNumberAsync(string participantId)
    {

        // JAMAL Code here - if this cannot lookup the participant, it should throw an exception (CornetException)
        logger.LogInformation($"*************************** ADD CS NUMBER LOOKUP CODE HERE ***************************");

        //var response = cornetORDSClient.GetCSNumberForParticipant(participantId);

        var responseModel = new ParticipantCSNumberModel
        {
            ParticipantId = participantId,
            CSNumber = GetRandomEightDigitNumber().ToString()
        };

        if (!string.IsNullOrEmpty(responseModel.ErrorMessage))
        {
            logger.LogWarning($"Error occurred getting CS Number for {participantId} {responseModel.ErrorMessage}");



            switch (responseModel.ErrorMessage)
            {

                case "MISC": // Missing CS Number
                    logger.LogWarning($"Participant {participantId} has no CS Number");
                    responseModel.ErrorMessage = "Participant {participantId} has no CS Number";
                    responseModel.ErrorType = CornetCSNumberErrorType.missingCSNumber;
                    break;
                case "NABI": // Missing BioMetric Registration
                    logger.LogWarning($"Participant {participantId} has no BioMetric Registration");
                    responseModel.ErrorMessage = "Participant {participantId} has no BioMetric Registration";
                    responseModel.ErrorType = CornetCSNumberErrorType.noActiveBioMetrics;
                    break;

                case "EDNP": // eDisclosure not provisioned
                    logger.LogWarning($"Participant {participantId} eDisclosure not provisioned");
                    responseModel.ErrorMessage = "Participant {participantId} eDisclosure not provisioned";
                    responseModel.ErrorType = CornetCSNumberErrorType.eDisclosureNotProvisioned;
                    break;

                case "OTHR": // Other errors
                    logger.LogWarning($"Participant {participantId} unknown error occurred");
                    responseModel.ErrorMessage = "Participant {participantId} unknown error occurred";
                    responseModel.ErrorType = CornetCSNumberErrorType.otherError;

                    break;

                default:
                    logger.LogWarning($"Participant {participantId} unhandled response returned {response.ErrorMessage}");
                    responseModel.ErrorMessage = "Participant {participantId} unhandled response returned {response.ErrorMessage}";
                    responseModel.ErrorType = CornetCSNumberErrorType.unknownResponseError;
                    break;

            }



            return responseModel;

        }

        /// <summary>
        /// Publish a CS number to Participant ID response mapping to outbound topic
        /// </summary>
        /// <param name="participantId"></param>
        /// <param name="csNumber"></param>
        /// <returns></returns>
        /// <exception cref="DIAMKafkaException"></exception>
        public async Task<Dictionary<string, string>> PublishCSNumberResponseAsync(string participantId)
        {
            try
            {
                var guid = Guid.NewGuid();
                var csNumberLookupResponse = await this.GetParticipantCSNumberAsync(participantId);

                var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, guid.ToString(), csNumberLookupResponse);


                if (response.Status != PersistenceStatus.Persisted)
                {
                    throw new DIAMKafkaException($"Failed to publish cs number mapping for {guid.ToString()} Part: {csNumberLookupResponse.ParticipantId} CSNumber: {csNumberLookupResponse.CSNumber}");
                }
                else
                {

                    return new Dictionary<string, string>
                {
                    { "id", guid.ToString() },
                    { "CSNumber", string.IsNullOrEmpty( csNumberLookupResponse.CSNumber) ? "NOT_FOUND": csNumberLookupResponse.CSNumber },
                  };
                }
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish cs number mapping");
                throw;
            }

        }


        /// <summary>
        /// Publish a notification within the CORNET system
        /// </summary>
        /// <param name="csNumber"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<int> SubmitNotificationToEServices(string csNumber, string message)
        {
            logger.LogInformation($"Publish notification for {csNumber} {message}");

            // cornetORDSClient.GetCSNumberToParticipant(csNumber, message);

            // JAMAL add code here
            logger.LogInformation($"*************************** ADD PUBLISH EVENT CODE HERE ***************************");

            // some response code to show notification worked
            return 0;

        }

        private static int GetRandomEightDigitNumber() => new Random().Next(10000000, 99999999);
    }
