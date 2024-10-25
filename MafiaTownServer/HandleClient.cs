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
                foreach (var player in Program.PlayerList)
                    if (clientName == player.Key && !player.Value.Alive)
                    {
                        return;
                    }
                ChatMessage? msg = clientSocket.ReadChatMessage();
                if (msg != null)
                {
                    if(msg.Sender == clientName) 
                    {
                        if(msg.Message[0] == '!') 
                        {
                            HandleCommand(msg);
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

    private static void HandleCommand(ChatMessage msg)
    {
        if (msg.Message[..5] == "!kill")
        {
            Program.KillPlayer(msg);    
            Console.WriteLine($"{msg.Sender} said: {msg.Message}");  
        }
        // if (msg.Message[..5] == "!vote")
        // {
        //     Program.Vote(msg);
        //     Console.WriteLine($"{msg.Sender} said: {msg.Message}");  
        // }
    }
}
