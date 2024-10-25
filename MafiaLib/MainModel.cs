using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace MafiaLib;

public class MainModel
{
    #region Properties and Fields

    public readonly string IP = Extensions.LocalIPAddress().ToString();
    public readonly int PORT = 8888;
    
    private TcpClient? _socket;
    
    private string _Username = string.Empty;
    public string Username 
    {
        get => _Username;
        set => SetField<string>(out _Username, value);
    }

    public bool Alive = true;

    public char Role = 'C';

    public bool Voted = false;

    private string _MessageBoard = string.Empty;
    public string MessageBoard { 
        get => _MessageBoard;
        set => SetField<string>(out _MessageBoard, value); 
    }

    private string _CurrentMessage = string.Empty;
    public string CurrentMessage
    {
        get => _CurrentMessage;
        set => SetField<string>(out _CurrentMessage, value);
    }

    private bool _Connected;
    public bool Connected
    {
        get => _Connected; 
        set => SetField<bool>(out _Connected, value);
    }
    #endregion

    #region Methods

    public void Connect()
    {
        _socket = new TcpClient();
        _socket.Connect(IP, PORT);
        Connected = true;
        Send();
        var thread = new Thread(GetMessage);
        thread.Start();
    }

    public void Send()
    {
        if (_socket != null)
        {
            if(CanChat()) {
                ChatMessage msg = new ChatMessage(_Username, _CurrentMessage);
                _socket.WriteChatMessage(msg);
            }
        }
    }

    public bool CanChat() 
    {
        if(!this.Alive)     // check for talking while dead
        {
            MessageBoard += $"You can't talk when you're dead.{Environment.NewLine}";
            return false;
        }
        if(_CurrentMessage.Length >= 5 && _CurrentMessage[..5] == "!vote")    // check for multiple voting
        {
            if(Voted)
            {
                MessageBoard += $"You alread voted.{Environment.NewLine}";
                return false;
            }
            else 
            {
                Voted = true;
                return true;
            }
        }
        return true;
    }

    public void GetMessage()
    {
        while (_socket != null) {
            ChatMessage? msg = _socket.ReadChatMessage();
            // put in logic for effect of message. e.g. !kill -> this._Alive = false
            if (msg!=null)
            {
                if(msg.Message.Length >= 5 && msg.Message[..5] == "!kill") {
                    this.Alive = false;
                    MessageBoard += $"{msg.Sender} just killed you! {Environment.NewLine}";
                }
                if(msg.Message.Length >= 6 && msg.Message[..6] == "!evict") {
                    this.Alive = false;
                    MessageBoard += $"You have been voted out. {Environment.NewLine}";
                }
                else {
                    MessageBoard += $"{msg.Sender} said: {msg.Message} {Environment.NewLine}";
                }
            }
        }
    }

    #endregion

    #region INPC

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(out T variable, T value, [CallerMemberName] string propertyName = "")
    {
        variable = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
