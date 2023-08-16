namespace ApprovalFlow.Features.WebSockets;

using System.Net.WebSockets;
using System.Text;

public class WebSocketService
{
    public static WebSocketService webSocketInstance = new WebSocketService();

    private WebSocketService() { }

    public static WebSocketService GetInstance() => webSocketInstance;

    private List<WebSocket> connections = new();

    public void AddConnection(WebSocket ws) => this.connections.Add(ws);


    public async Task Broadcast(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var connection in this.connections)
        {
            if (connection != null && connection.State == WebSocketState.Open)
            {
                Serilog.Log.Information($"Broadcast to {connection}");
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await connection.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
