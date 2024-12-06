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
        string command = parsed[0];
        Player target = GetPlayer(parsed[1]);

        if(target is not null && sender.Alive == true)
        {
            // TODO: Clean up logic
            if(GameState.CurrentPhase == Phase.NIGHT)
            {
                NightCmds(sender, target, command);
            }
            else if (GameState.CurrentPhase == Phase.VOTE && command == "!vote" && target.Alive == true)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You voted for {target.Name}."));
                GameState.Vote(sender, target);
            }
            else
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You can't use commands right now.")); 
            }
        }
        else {
            Program.SendTo(sender.Client, new ChatMessage("System", $"No such player '{parsed[1]}'."));
            return;
        }
    }

    private void NightCmds(Player sender, Player target, string cmd)
    {
        // !kill command
        if (cmd == "!kill")
        {
            if (sender.Role == Role.MAFIA && target.Alive == true)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You targeted {target.Name}."));
                GameState.Target(sender, target);
            }
            else if (sender.Role == Role.MAFIA && target.Alive == false)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"Target is already dead."));
            }
            else if (sender.Role != Role.MAFIA)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You can't do that."));
            }
        }

        // !heal command
        if (cmd == "!heal")
        {
            if (sender.Role == Role.DOCTOR && target.Alive == true)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You healed {target.Name}."));
                GameState.Heal(sender, target);
            }
            else if (sender.Role == Role.DOCTOR && target.Alive == false)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"Target is already dead."));
            }
            else if (sender.Role != Role.DOCTOR)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You can't do that."));
        }
        }

        // !check command
        if (cmd == "!check")
        {
            if (sender.Role == Role.SHERIFF && target.Alive == true)
            {
                GameState.Investigate(sender);
                Program.SendTo(sender.Client, new ChatMessage("System", $"You investigated {target.Name}. Their role is {target.Role}."));
            }
            else if (sender.Role == Role.SHERIFF && target.Alive == false)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"Target is dead."));
            }
            else if (sender.Role != Role.SHERIFF)
            {
                Program.SendTo(sender.Client, new ChatMessage("System", $"You can't do that."));
            }
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
