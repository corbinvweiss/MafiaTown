using System;
using System.ComponentModel;
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
        GameState = gameState;
        GameState.PropertyChanged += OnPhaseChanged;
    }

    private void OnPhaseChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is GameState gameState)
        {
            if(e.PropertyName == "CurrentPhase")
            {
                Console.WriteLine($"Player {clientName} notified of phase change: {gameState.CurrentPhase}.");
            }
        }
    }

    public void StartClient()
    {
        var thread = new Thread(Start);
        thread.Start();
    }

    public void Start() 
    {
        Program.SendTo(clientSocket, new ChatMessage("System", $"You are a {player.Role}"));
        while(true)
        {
            ChatMessage? msg = clientSocket.ReadChatMessage();
            if(msg?.Message[0] != '!') 
            {
                DoChat(msg);
            } 
            if(GameState.CurrentPhase == Phase.NIGHT)
            {
                Night(msg);
            }
            if(GameState.CurrentPhase == Phase.NOMINATE)
            {
                Nominate(msg);
            }
            if(GameState.CurrentPhase == Phase.VOTE)
            {
                Vote(msg);
            }
            if(GameState.CurrentPhase == Phase.END)
            {
                End(msg);
            }
        }
    }

    private void Night(ChatMessage msg)
    {
        Program.SendTo(clientSocket, new ChatMessage("System", "Night has fallen."));
        // TODO: send instructions to the appropriate players
        try
        {
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
                        player.FinishPhase();
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

    private void Nominate(ChatMessage msg) 
    {
        // TODO: notify the players of their roles.
        Program.SendTo(clientSocket, new ChatMessage("System", $"Nominate"));
        DoChat(msg);
    }

    private void Vote(ChatMessage msg) 
    {
        // TODO: notify the players of their roles.
        Program.SendTo(clientSocket, new ChatMessage("System", $"Vote"));
        DoChat(msg);
    }

    private void End(ChatMessage msg) 
    {
        // TODO: notify the players of their roles.
        Program.SendTo(clientSocket, new ChatMessage("System", $"End"));
        DoChat(msg);
    }

    private void DoChat(ChatMessage msg)
    {
        try
        {
            if (msg != null)
            {
                if(msg.Sender == clientName) 
                {
                    Program.Broadcast(msg);
                    Console.WriteLine($"{clientName} said: {msg.Message}");
                    if(msg.Message == "ready")
                    {
                        player.FinishPhase();
                    }
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
