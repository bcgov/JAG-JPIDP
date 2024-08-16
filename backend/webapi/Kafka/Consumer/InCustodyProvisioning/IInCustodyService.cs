namespace Pidp.Kafka.Consumer.InCustodyProvisioning;

using Common.Models.CORNET;

public interface IInCustodyService
{
    // process an incoming message
    Task<Task> ProcessInCustodySubmissionMessage(InCustodyParticipantModel value);

}
