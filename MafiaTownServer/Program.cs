using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MafiaLib;

namespace MafiaTownServer;

public class Player
{
    public TcpClient Client;
    public string Role = String.Empty;
    public bool Voted = false;
    public bool Alive = true;
    public int VotesAgainst = 0;

    public Player(TcpClient tcpClient, string role)
    {
        Client = tcpClient;
        Role = role;
    }

}

public static class Program
{
    public static Dictionary<string, Player> PlayerList = new Dictionary<string, Player>();

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
                    Player player = new Player(clientSocket, "Civilian");
                    PlayerList.Add(joinMessage.Sender, player);
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
        foreach (var item in PlayerList)
        try 
        {
            item.Value.Client.WriteChatMessage(msg);
        }
        catch
        {
            Console.WriteLine($"Unable to write to user {item.Key}, removing user.");
            PlayerList.Remove(item.Key);
        }
    }

    // Send the message to a particular person
    public static void Send(ChatMessage msg)
    {
        foreach (var item in PlayerList)
        {
            if(item.Key == msg.Sender) 
            {
                try 
                {
                    item.Value.Client.WriteChatMessage(msg);
                }
                catch
                {
                    Console.WriteLine($"Unable to write to user {item.Key}, removing user.");
                    PlayerList.Remove(item.Key);
                }
            }
        }
    }

    internal static void Vote(ChatMessage msg)  // track vote of msg.Sender for msg.Message
    {
        foreach (var item in PlayerList)
        {
            if(item.Key == msg.Message[6..])
            {
                item.Value.VotesAgainst++;  // update the votecount
            }
            if(item.Key == msg.Sender)
            {
                item.Value.Voted = true;    // update who has voted
            }
        }
    }

    internal static void KillPlayer(ChatMessage msg)
    {
        foreach (var item in PlayerList)
        {
            if(item.Key == msg.Message[6..])
            {
                try 
                {
                    item.Value.Client.WriteChatMessage(msg);
                    item.Value.Alive = false;
                }
                catch
                {
                    Console.WriteLine($"Unable to write to user {item.Key}, removing user.");
                    PlayerList.Remove(item.Key);
                }
            }
            if(item.Key == msg.Sender)  // notify the killer they have killed the target
            {
                try
                {
                    ChatMessage notify = new ChatMessage("System", $"You have killed {msg.Message[6..]}");
                    item.Value.Client.WriteChatMessage(notify);
                }
                catch
                {
                    Console.WriteLine($"Unable to write to user {item.Key}, removing user.");
                    PlayerList.Remove(item.Key);
                }
            }
        }
    }
}
