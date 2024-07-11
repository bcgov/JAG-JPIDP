namespace DIAMCornetService.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationContoller(Services.INotificationService notificationService) : ControllerBase
{

    [HttpGet(Name = "GenerateTestNotification")]
    public async Task<ActionResult<string>> PublishTestNotification([FromQuery] string participantId, [FromQuery] string messageText)
    {
        try
        {
            var publishedGUID = await notificationService.PublishTestNotificationAsync(participantId, messageText);
            return publishedGUID;
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, $"Failed to publish test notification: {ex.Message}");
        }
    }
}
