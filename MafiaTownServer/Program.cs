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
            try
            {
                var clientSocket = ServerSocket.AcceptTcpClient();
                ChatMessage? joinMessage = clientSocket.ReadChatMessage();
                if (joinMessage != null) 
                {
                    // add the player to the GameState
                    Player player = new(joinMessage.Sender, clientSocket);
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


    public static void AssignRoles() 
    {
        Role[] roles = [Role.CIVILIAN, Role.DOCTOR, Role.MAFIA, Role.CIVILIAN];
        int i = 0;
        foreach (var player in GameState.PlayerList) 
        {
            player.SetRole(roles[i]);
            SendTo(player.Client, new ChatMessage("System", $"You have been assigned the role {roles[i]}."));
            i++;
        }
    }

    public static void Done(string username) // cause username to finish their actions in this phase
    {
        Console.WriteLine("In Program.Done");
        Console.WriteLine(GameState.PlayerList);
        Console.WriteLine(GameState.PlayerList.Count);
        for(int i=0; i<GameState.PlayerList.Count; i++)
        {
            if(GameState.PlayerList[i].Name == username)
            {
                Console.WriteLine("About to finish phase");
                GameState.PlayerList[i].FinishPhase();
            }
        }
        Console.WriteLine("finished finishing phase");
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
