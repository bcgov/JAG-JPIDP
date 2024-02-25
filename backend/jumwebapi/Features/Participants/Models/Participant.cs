using Common.Models.JUSTIN;

namespace jumwebapi.Features.Participants.Models
{
  // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);




  public class Party
  {
    public Participant participant { get; set; } = new Participant();
  }


}
