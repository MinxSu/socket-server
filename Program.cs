using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;
class Program
{
    static async Task Main(string[] args)
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 8080);
        TcpListener listener = new(ipEndPoint);

        try
        {
            listener.Start();

            using TcpClient handler = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = handler.GetStream();

            var message = $"Receive Message At: {DateTime.Now}";
            Console.Write(message);
            var dateTimeBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dateTimeBytes);
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
        finally
        {
            // listener.Stop();
        }
    }
}
