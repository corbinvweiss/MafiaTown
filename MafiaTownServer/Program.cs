using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MafiaLib;

namespace MafiaTownServer;

public static class Program
{
    public static GameState GameState { get; private set; } = new GameState();
    public static void Main()
    {
        var ServerSocket = new TcpListener(IPAddress.Any, 8888);
        ServerSocket.Start();
        Console.WriteLine("Mafia server has started.");
        while (true)
        {
            Role nextRole = GetNextRole();
            try
            {
                var clientSocket = ServerSocket.AcceptTcpClient();
                ChatMessage? joinMessage = clientSocket.ReadChatMessage();
                if (joinMessage != null) 
                {
                    // add the player to the GameState, assigning a role as they come in.
                    Player player = new(joinMessage.Sender, clientSocket, nextRole);
                    GameState.AddPlayer(player);
                    // add the client handle to the server.
                    var client = new HandleClient(player, GameState);
                    Broadcast(new ChatMessage("System", $"{joinMessage.Sender}"));
                    client.StartClient();
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Some client aborted connection: {ex.Message}");
                //TODO: probably remove the offending client
            }
        }
    }

    public static Role GetNextRole()
    {
        // give the first player Mafia, the second doctor, the third sheriff, and all the rest civilian
        Role[] roles = [Role.MAFIA, Role.DOCTOR, Role.SHERIFF, Role.CIVILIAN];
        if(GameState.PlayerList.Count < 4) 
        {
            return roles[GameState.PlayerList.Count];
        }
        else
        {
            return Role.CIVILIAN;
        }
    }


    public static void SendTo(TcpClient recipient, ChatMessage msg)
    {
        try 
        {
            recipient.WriteChatMessage(msg);
        }
        catch   // if you can't reach the recipient, remove them.
        {
            foreach (var player in GameState.PlayerList)
            {
                if(player.Client == recipient)
                {
                    Console.WriteLine($"Unable to write to user {player.Name}, removing user.");
                    GameState.RemovePlayer(player);
                }
            }
            
        }
    }

    public static void Broadcast(ChatMessage msg)
    {
        foreach (var player in GameState.PlayerList)
        try {
            player.Client.WriteChatMessage(msg);
        }
        catch
        {
            Console.WriteLine($"Unable to write to user {player.Name}, removing user.");
            
            GameState.RemovePlayer(player);
        }
    }
}
