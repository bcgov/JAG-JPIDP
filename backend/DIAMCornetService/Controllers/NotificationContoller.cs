namespace DIAMCornetService.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class NotificationContoller : ControllerBase
{
    private readonly ILogger<NotificationContoller> _logger;
    private readonly DIAMCornetService.Services.INotificationService _notificationService;

    public NotificationContoller(ILogger<NotificationContoller> logger, DIAMCornetService.Services.INotificationService notificationService)
    {
        this._logger = logger;
        this._notificationService = notificationService;
    }

    [HttpGet(Name = "GenerateTestNotification")]
    public async Task<ActionResult<string>> PublishTestNotification([FromQuery] string participantId, [FromQuery] string messageText)
    {
        try
        {
            var publishedGUID = await this._notificationService.PublishTestNotificationAsync(participantId, messageText);
            return publishedGUID;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish test notification");
            return StatusCode(500, $"Failed to publish test notification: {ex.Message}");
        }
    }
}
