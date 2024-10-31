using System.ComponentModel.DataAnnotations;
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

    public static void SendTo(string username, TcpClient client, ChatMessage msg)
    {
        try 
        {
            client.WriteChatMessage(msg);
        }
        catch
        {
            Console.WriteLine($"Unable to write to user {username}, removing user.");
            PlayerList.Remove(username);
        }
    }

    public static void Broadcast(ChatMessage msg)
    {
        foreach (var item in PlayerList)
        SendTo(item.Key, item.Value.Client, msg);
    }

    // Send the message to a particular person
    public static void Send(ChatMessage msg)
    {
        foreach (var item in PlayerList)
        {
            if(item.Key == msg.Sender) 
            {
                SendTo(item.Key, item.Value.Client, msg);
            }
        }
    }

    internal static void Vote(ChatMessage msg)  // track vote of msg.Sender for msg.Message
    {
        Broadcast(new ChatMessage("System", "State=VOTING"));
        KeyValuePair<string, Player> target = PlayerList.First();
        KeyValuePair<string, Player> sender = PlayerList.First();
        bool foundPlayer = false;
        foreach (var item in PlayerList)
        {
            if(item.Key == msg.Message[6..])
            {
                target = item;
                foundPlayer = true;
            }
            if(item.Key == msg.Sender)
            {
                sender = item;
            }
        }
        // if(sender.Value.Voted) 
        // {
        //     SendTo(sender.Key, sender.Value.Client, new ChatMessage("System", "You already voted this round."));
        // }
        if(foundPlayer)
        {
            if(target.Value.Alive) // if the player voted for exists and is alive, tally their votes
            {
                target.Value.VotesAgainst++;
                sender.Value.Voted = true;
                ChatMessage notify = new ChatMessage("System", $"You have voted for {msg.Message[6..]}.");
                SendTo(sender.Key, sender.Value.Client, notify);
                TallyVote();
            }
            else    // if the player voted for exists and is dead, notify the voter.
            {
                SendTo(sender.Key, sender.Value.Client, new ChatMessage("System", $"{msg.Message[6..]} is dead."));
            }
        }
        else    // if the player voted for does not exist notify teh voter.
        {
            SendTo(sender.Key, sender.Value.Client, new ChatMessage("System", $"No such player {msg.Message[6..]}."));
        }
    }

    internal static void TallyVote() 
    {
        KeyValuePair<string, Player> mostVoted = PlayerList.First();
        bool tie = false;
        foreach (var item in PlayerList)    // find the most voted for player
        {
            if(item.Value.Alive && !item.Value.Voted)   // if anyone alive has not voted, stop tallying votes.
            {
                return;
            }
            if(item.Value.VotesAgainst > mostVoted.Value.VotesAgainst) 
            {
                mostVoted = item;
            }
        }
        foreach (var item in PlayerList)    // find the second most voted for player
        {
            if(item.Key != mostVoted.Key && item.Value.VotesAgainst == mostVoted.Value.VotesAgainst) 
            {
                tie = true;
            }
        }
        if(tie)
        {
            Broadcast(new ChatMessage("System", "The vote ended in a tie."));
        }
        else
        {
            // kick out the player.
            mostVoted.Value.Alive = false;
            SendTo(mostVoted.Key, mostVoted.Value.Client, new ChatMessage("System", "!evict"));

            // notify the other players.
            ChatMessage notify = new ChatMessage("System", $"{mostVoted.Key} has been voted out.");
            foreach (var item in PlayerList)
            {
                if(item.Key != mostVoted.Key) 
                {
                    SendTo(item.Key, item.Value.Client, notify);
                }
            }
        }
        Broadcast(new ChatMessage("System", "State=CHAT"));
    }

    internal static void KillPlayer(ChatMessage msg)
    {
        KeyValuePair<string, Player> target = PlayerList.First();
        KeyValuePair<string, Player> sender = PlayerList.First();
        foreach (var item in PlayerList) // find the sender and the target in the playerlist
        {
            if(item.Key == msg.Message[6..])
            {
                target = item;
            }
            if(item.Key == msg.Sender)
            {
                sender = item;
            }
        }

        ChatMessage notify = new ChatMessage("System", $"You have killed {target.Key}.");
        if(!target.Value.Alive) 
        {
            notify = new ChatMessage("System", $"{target.Key} is already dead.");
        }
        if(target.Key != sender.Key){   // if it is not a self-kill, then notify the sender
            SendTo(sender.Key, sender.Value.Client, notify);
        }
        if(target.Value.Alive)  // Execute the player if they're not already dead
        {
            SendTo(target.Key, target.Value.Client, msg);
            target.Value.Alive = false;
        }
    }
}
