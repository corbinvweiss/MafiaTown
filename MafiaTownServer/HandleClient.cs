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
                Program.SendTo(clientSocket, new ChatMessage("System", $"{gameState.CurrentPhase}"));
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
            if(msg?.Message.Length > 0 && msg?.Message[0] != '!') 
            {
                DoChat(msg);
            } 
            else if(msg?.Message.Length > 0 && msg?.Message[0] == '!')
            {
                HandleCommand(msg);
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
            if(msg.Message[..5] == "!kill" && sender.Role == Role.MAFIA && GameState.CurrentPhase == Phase.NIGHT)
            {
                GameState.Target(sender, target);
                Program.SendTo(sender.Client, new ChatMessage("System", $"You have killed {target.Name}."));
            }
            else
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You can't do that."));
            }
        }
        else {
            Program.SendTo(sender.Client, new ChatMessage("System", $"No such player '{parsed[1]}'."));
            return;
        }
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
