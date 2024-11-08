using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;
using MafiaLib;
using Microsoft.VisualBasic;

namespace MafiaTownServer;

internal class HandleClient
{
    private TcpClient clientSocket;
    private string clientName;
    private readonly Player player;
    private GameState GameState;
    public HandleClient(Player assignedPlayer, GameState gameState) 
    {
        if (assignedPlayer.Client is null || string.IsNullOrWhiteSpace(assignedPlayer.Name))
        {
            throw new ArgumentNullException("socket and string must not be null or empty.");
        }
        player = assignedPlayer;
        clientSocket = assignedPlayer.Client;
        clientName = assignedPlayer.Name;
        // subscribe to GameState update events
        gameState.PhaseChanged += OnPhaseChanged;
        gameState.PlayerAdded += OnPlayerAdded;
        player.PlayerStateChanged += OnPlayerStateChanged;
    }

    private void OnPhaseChanged(Phase newPhase)
    {
        Console.WriteLine($"Player {clientName} notified of phase change: {newPhase}.");
        if(newPhase == Phase.NIGHT)
        {
            Night();
        }
    }

    private void OnPlayerAdded(Player player)
    {
        Console.WriteLine($"Player {clientName} notified of {player.Name} added.");
    }

    private void OnPlayerStateChanged(Player player, string propertyName)
    {
        Console.WriteLine($"Player {player.Name}'s {propertyName} has changed.");
    }

    public void StartClient()
    {
        var thread = new Thread(DoChat);
        thread.Start();
    }

    private void Night()
    {
        Program.Broadcast(new ChatMessage("System", "Night has fallen."));
        // TODO: send instructions to the appropriate players
        while(true)
        {
            try
            {
                ChatMessage? msg = clientSocket.ReadChatMessage();
                if (msg != null)
                {
                    if(msg.Sender == clientName) 
                    {
                        if(msg.Message[0] == '!')
                        {
                            HandleCommand(msg);
                        }
                        if(msg.Message == "ready")
                        {
                            Program.Done(msg.Sender);
                        }
                        Program.Broadcast(msg);
                        Console.WriteLine($"{clientName} said: {msg.Message}");
                    }
                }
                else 
                {
                    Console.WriteLine($"{clientName} did not get a message because it was ill formed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
            }
        }
    }

    private Player GetPlayer(string name)
    {
        Player? p = null;
        foreach(Player player in GameState.PlayerList)
        {
            if(player.Name == name)
            {
                p = player;
            }
        }
        return p;
    }

    // handle a command such as !kill or !heal or !vote or !check
    private void HandleCommand(ChatMessage msg)
    {
        // get the sender and the target
        Player sender = GetPlayer(msg.Sender);
        string[] parsed =  msg.Message.Split(' ');
        if(parsed.Length < 2)
        {
            if(sender is not null) 
            { 
                Program.SendTo(sender.Client, new ChatMessage("System", $"No target designated.")); 
                return;
            }
        }
        Player target = GetPlayer(parsed[1]);

        if(target is not null){
            if(msg.Message[..5] == "!kill" && sender.Role == Role.MAFIA)
            {
                GameState.Target(sender, target);
            }
        }
        else {
            Program.SendTo(sender.Client, new ChatMessage("System", $"No such player '{parsed[1]}'."));
            return;
        }
    }

    private void DoChat() 
    {
        while(true)
        {
            try
            {
                ChatMessage? msg = clientSocket.ReadChatMessage();
                if (msg != null)
                {
                    if(msg.Sender == clientName) 
                    {
                        if(msg.Message == "ready")
                        {
                            Program.Done(msg.Sender);
                        }
                        Program.Broadcast(msg);
                        Console.WriteLine($"{clientName} said: {msg.Message}");
                    }
                }
                else 
                {
                    Console.WriteLine($"{clientName} did not get a message because it was ill formed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
            }
        }
    }
}
