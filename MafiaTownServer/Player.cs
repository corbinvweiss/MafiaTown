using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace MafiaTownServer;

public enum Role
{
    MAFIA,
    CIVILIAN,
    DOCTOR,
    SHERIFF
}

public class Player
{
    // Event triggered whenever the player state changes
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name {get; }
    public TcpClient Client{get; }
    public Role Role {get; }

    private bool _Alive = true;
    public bool Alive
    {
        get => _Alive;
        set => SetField<bool>(out _Alive, value);
    }
    private bool _Voted = false;
    public bool Voted
    {
        get => _Voted;
        set => SetField<bool>(out _Voted, value);
    }

    private int _VotesAgainst = 0;
    public int VotesAgainst
    {
        get => _VotesAgainst;
        set => SetField<int>(out _VotesAgainst, value);
    }
    private bool _Targeted = false;
    public bool Targeted
    {
        get => _Targeted;
        set => SetField<bool>(out _Targeted, value);
    }
    private bool _Healed = false;
    public bool Healed
    {
        get => _Healed;
        set => SetField<bool>(out _Healed, value);
    }
    private bool _Done = false;
    public bool Done
    {
        get => _Done;
        set => SetField<bool>(out _Done, value);
    }

    public Player(string name, TcpClient client, Role role)
    {
        Name = name;
        Client = client;
        Role = role;
    }

    public void Kill()
    {
        Alive = false;
    }

    public void MarkAsVoted()
    {
        Voted = true;
    }

    public void AddVoteAgainst()
    {
        VotesAgainst++;
    }

    public void Heal()
    {
        Healed = true;
    }

    public void Target()
    {
        Targeted = true;
    }
    public void FinishPhase()
    {
        Done = true;
    }

    public void StartPhase()
    {
        Done = false;
    }

    protected void SetField<T>(out T variable, T value, [CallerMemberName] string propertyName = "")
    {
        variable = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
