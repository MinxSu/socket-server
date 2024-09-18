using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

class WebSocketServer
{
    private static ConcurrentDictionary<Guid, WebSocket> _clients = new ConcurrentDictionary<Guid, WebSocket>();

    static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://*:8766/");
        listener.Start();
        Console.WriteLine("WebSocket Server Start: 8766...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        WebSocket webSocket = webSocketContext.WebSocket;

        var clientId = Guid.NewGuid();
        _clients.TryAdd(clientId, webSocket);
        Console.WriteLine($"client connected: {clientId}");

        try
        {
            await HandleWebSocketConnection(webSocket, clientId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket Error: {ex.Message}");
        }
        finally
        {
            try
            {
                _clients.TryRemove(clientId, out _);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine($"client disconnected: {clientId}");
            }
            catch
            {
                Console.WriteLine($"disconnected failed: {clientId}");
            }
        }
    }

    private static async Task HandleWebSocketConnection(WebSocket webSocket, Guid clientId)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received Message: {message}");

                // reply
                string reply = $"Server Received: {message}";
                await SendMessageAsync(webSocket, reply);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }
    }

    private static async Task SendMessageAsync(WebSocket socket, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}