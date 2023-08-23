namespace ApprovalFlow.Features.WebSockets;

using System.Net.WebSockets;
using Common.Models.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class WebSocketController : ControllerBase
{
    private WebSocketService _websocketService = WebSocketService.GetInstance();

    [HttpGet("/ws")]
    public async Task Get()
    {
        if (this.HttpContext.WebSockets.IsWebSocketRequest)
        {
            var protocol = this.HttpContext.WebSockets.WebSocketRequestedProtocols[0];

            using var webSocket = await this.HttpContext.WebSockets.AcceptWebSocketAsync(protocol);
            this._websocketService.AddConnection(webSocket);
            await webSocket.SendAsync(WSMessage.GetMessage(MessageType.Broadcast, "Connected", null),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
            await Echo(webSocket);
        }
        else
        {
            this.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }


    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
