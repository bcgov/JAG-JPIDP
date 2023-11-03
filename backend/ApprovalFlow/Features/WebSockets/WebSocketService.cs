namespace ApprovalFlow.Features.WebSockets;

using System.Net.WebSockets;
using Common.Models.WebSocket;

public class WebSocketService
{
    public static WebSocketService webSocketInstance = new WebSocketService();

    private WebSocketService() { }

    public static WebSocketService GetInstance() => webSocketInstance;

    private List<WebSocket> connections = new();

    public void AddConnection(WebSocket ws) => this.connections.Add(ws);


    public async Task Broadcast(WSMessage message)
    {

        foreach (var connection in this.connections)
        {
            if (connection != null && connection.State == WebSocketState.Open)
            {
                Serilog.Log.Information($"Broadcast to {connection}");
                await connection.SendAsync(message.GetMessageSegment(), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
