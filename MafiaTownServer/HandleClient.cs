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
                ChatMessage notify = GetPhaseInstructions();
                Console.WriteLine($"Player {clientName} notified of phase change: {gameState.CurrentPhase}.");
                Program.SendTo(clientSocket, notify);
            }
        }
    }

    private ChatMessage GetPhaseInstructions()
    {
        string instructions = "";
        if(GameState.CurrentPhase == Phase.NIGHT)
        {
            instructions = "Night has fallen. The mafia will decide who to kill, \n"
                +"the doctor who to heal, and the sheriff who to investigate. \n"
                +"The rest of y'all can just chat.\n\n"
                +"Here are your commands:\n"
                +"MAFIA: !kill <player>\n"
                +"DOCTOR: !heal <player>\n"
                +"SHERIFF: !check <player>\n";
        }
        else if(GameState.CurrentPhase == Phase.VOTE)
        {
            instructions = "Good morning folks! It's time for the news from the night:\n"
                + GameState.WhatHappened + "\n"
                + "Now work together to decide who to vote out of this game.\n\n"
                + "Here's the command to vote:\n"
                + "!vote <player>\n";
        }
        else if(GameState.CurrentPhase == Phase.END)
        {
            instructions = "Welp, that's a wrap. GG to y'all.\n"
                + GameState.WhatHappened + "\n"
                + "Feel free to stick around and chat, or just leave\n\n"
                + "If you can.\n";
        }
        return new("System", instructions);
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
