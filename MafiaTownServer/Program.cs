using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MafiaLib;

namespace MafiaTownServer;

public static class Program
{
    public static Dictionary<string, TcpClient> ClientList = new Dictionary<string, TcpClient>();

    public static void Main()
    {
        var ServerSocket = new TcpListener(IPAddress.Any, 8888);
        ServerSocket.Start();
        Console.WriteLine("Mafia server has started.");
        while (true)
        {
            try
            {
                var clientSocket = ServerSocket.AcceptTcpClient();
                ChatMessage? joinMessage = clientSocket.ReadChatMessage();
                if (joinMessage != null) 
                {
                    ClientList.Add(joinMessage.Sender, clientSocket);
                    Broadcast(new ChatMessage("System", $"{joinMessage.Sender}"));
                    var client = new HandleClient(clientSocket, joinMessage.Sender);
                    client.StartClient();
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Some client aborted connection: {ex.Message}");
                //TODO: probably remove the offending client
            }
        }
    }

    public static void Broadcast(ChatMessage msg)
    {
        foreach (var item in ClientList)
        try {
            item.Value.WriteChatMessage(msg);
        }
        catch
        {
            Console.WriteLine($"Unable to write to user {item.Key}, removing user.");
            ClientList.Remove(item.Key);
        }
    }
}
