using System;
using System.Net.Security;
using System.Net.Sockets;
using MafiaLib;

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
            Program.Broadcast(new ChatMessage("System", "Night has fallen."));
            DoChat();
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

    private void Play()
    {
        Program.Broadcast(new ChatMessage("System", "Starting the game!"));
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
                        if(msg.Message == "done")
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
