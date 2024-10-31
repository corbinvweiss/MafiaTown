using System;
using System.Net.Sockets;
using MafiaLib;

namespace MafiaTownServer;

internal class HandleClient
{
    private TcpClient clientSocket;
    private string clientName;
    public HandleClient(TcpClient socket, string name) 
    {
        if (socket is null || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException("socket and string must not be null or empty.");
        }
        clientSocket = socket;
        clientName = name;
    }

    public void StartClient()
    {
        var thread = new Thread(DoChat);
        thread.Start();
    }

    private void DoChat() 
    {
        while (true)
        {
            try
            {
                ChatMessage? msg = clientSocket.ReadChatMessage();
                if (msg != null)
                {
                    if(!Program.PlayerList[msg.Sender].Alive) // check whether the player is alive
                    {
                        Program.SendTo(msg.Sender, Program.PlayerList[msg.Sender].Client, new ChatMessage("System", "You can't talk when you're dead."));
                        continue;
                    }
                    if(msg.Sender == clientName) 
                    {
                        if(msg.Message[0] == '!') 
                        {
                            HandleCommand(msg, Program.PlayerList[msg.Sender].Role);
                        }
                        else 
                        {
                            Program.Broadcast(msg);
                            Console.WriteLine($"{clientName} said: {msg.Message}");
                        }
                    }
                }
                else 
                {
                    Console.WriteLine($"{clientName} did not get a message because it was ill formed.");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                break;
            }
        }
    }

    private static void HandleCommand(ChatMessage msg, string role)
    {
        if (msg.Message.Length >= 5 && msg.Message[..5] == "!kill" && role == "M" && Program.CurrentState == State.NIGHT)
        {
            Program.KillPlayer(msg);    
            Console.WriteLine($"{msg.Sender} said: {msg.Message}");  
        }
        else
        {
            Console.WriteLine("You can't do that right now.");
        }

        if (msg.Message.Length >= 5 && msg.Message[..5] == "!vote" && Program.CurrentState == State.NIGHT)
        {
            Program.Vote(msg);
            Console.WriteLine($"{msg.Sender} said: {msg.Message}");  
        }
        else
        {
            Console.WriteLine("You can't do that right now.");
        }
    }
}
