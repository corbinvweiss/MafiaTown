using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace MafiaTownServer;

public enum Phase // track the phase of the game
{ 
    START,
    NIGHT,
    NOMINATE,
    VOTE,
    END
}

public enum Role
{
    MAFIA,
    CIVILIAN,
    DOCTOR,
    SHERIFF
}

public class ThreadSafeProperty<T>(T initialValue)
{
    private T value = initialValue;
    private readonly object lockObj = new object();

    public T Value
    {
        get
        {
            lock (lockObj)
            {
                return value;
            }
        }
        set
        {
            lock (lockObj)
            {
                this.value = value;
            }
        }
    }
}

public class PlayerChangedEventArgs : EventArgs
{
    public Player player { get; set; }
    public string PropertyName { get; set; }
}

public class Player
{
    // Event triggered whenever the player state changes
    public event EventHandler<PlayerChangedEventArgs>? PlayerStateChanged;
    public void OnPlayerStateChanged(string propertyName)
    {
        PlayerStateChanged?.Invoke(this, new PlayerChangedEventArgs {player = this, PropertyName = propertyName });
    }
    public string Name {get; }
    public TcpClient Client{get; }
    public Role Role {get; }
    public ThreadSafeProperty<bool> Alive {get; private set; }
    public ThreadSafeProperty<bool> Voted {get; private set; }      // has this player voted this round?
    public ThreadSafeProperty<int> VotesAgainst {get; private set; }
    public ThreadSafeProperty<bool> Targeted {get; private set; }   // has the mafia targeted this player?
    public ThreadSafeProperty<bool> Healed {get; private set; }     // has the doctor healed this player?
    public ThreadSafeProperty<bool> Done {get; private set; }  // indicates whether this player has done their action this round

    public Player(string name, TcpClient client, Role role)
    {
        Name = name;
        Client = client;
        Role = role;
        Alive = new ThreadSafeProperty<bool>(true);
        Voted = new ThreadSafeProperty<bool>(false);
        VotesAgainst = new ThreadSafeProperty<int>(0);
        Targeted = new ThreadSafeProperty<bool>(false);
        Healed = new ThreadSafeProperty<bool>(false);
        Done = new ThreadSafeProperty<bool>(false);
    }

    public void Kill()
    {
        Alive.Value = false;
        OnPlayerStateChanged(nameof(Alive));
    }

    public void MarkAsVoted()
    {
        Voted.Value = true;
        OnPlayerStateChanged(nameof(Voted));
    }

    public void AddVoteAgainst()
    {
        VotesAgainst.Value++;
        OnPlayerStateChanged(nameof(VotesAgainst));
    }

    public void Heal()
    {
        Healed.Value = true;
        OnPlayerStateChanged(nameof(Healed));
    }

    public void Target()
    {
        Targeted.Value = true;
        OnPlayerStateChanged(nameof(Targeted));
    }
    public void FinishPhase()
    {
        Done.Value = true;
        OnPlayerStateChanged("Done");
    }

    public void StartPhase()
    {
        Done.Value = false;
        OnPlayerStateChanged("Done");
    }
}

public class PhaseChangedEventArgs : EventArgs
{
    public Phase NewPhase { get; set; }
}

public class GameState  // global synchronized state of the game.
{
    public object StateLock { get; } = new object();    // lock to synchronize access to GameState

    #region Player State
    public List<Player> PlayerList {get; private set; } = new List<Player>();

    

    public void AddPlayer(Player newPlayer)
    {
        lock (StateLock)
        {
            PlayerList.Add(newPlayer);
        }
        // subscribe to changes in the player's state
        newPlayer.PlayerStateChanged += OnPlayerStateChanged;
    }

    public void RemovePlayer(Player p)
    {
        lock (StateLock)
        {
            PlayerList.Remove(p);
        }
    }

    public void Vote(Player voter, Player target)
    {
        voter.MarkAsVoted();
        target.AddVoteAgainst();
        voter.FinishPhase();
    }

    public void Heal(Player doctor, Player target)
    {
        target.Heal();
        doctor.FinishPhase();
    }

    public void Target(Player mafia, Player target)
    {
        target.Target();
        mafia.FinishPhase();
    }

    public void Investigate(Player sheriff)
    {
        sheriff.FinishPhase();
    }

    public void Kill(Player player)
    {
        player.Kill();
    }

    #endregion

    #region Game Phase
    public event EventHandler<PhaseChangedEventArgs>? PhaseChanged;    // when the phase changes, trigger an event
    private Phase currentPhase = Phase.START;

    public Phase CurrentPhase
    {
        get // synchronize reading of CurrentPhase so it's not changed while being read
        {
            lock (StateLock)
            {
                return currentPhase;
            }
        }
        set // synchronize writing of CurrentPhase to avoid race conditions
        {
            Phase notifyPhase;
            lock (StateLock)
            {
                if (currentPhase != value)
                {
                    currentPhase = value;
                    notifyPhase = currentPhase;
                    foreach(var d in PhaseChanged.GetInvocationList())
                    {
                        Console.WriteLine($"Subscriber: {d.Target}");
                    }
                    // fire PhaseChanged event to notify clients of new phase.
                    PhaseChanged?.Invoke(this, new PhaseChangedEventArgs { NewPhase = currentPhase });  
                    ResetPlayersReady(); // todo: reset players' readiness
                }
                else
                {
                    return; // no change, so no event
                }
            }
        }
    }

    private void ResetPlayersReady()
    {
        foreach (Player player in PlayerList)
        {
            player.StartPhase();
        }
    }

    private void OnPlayerStateChanged(object sender, PlayerChangedEventArgs e)
    {
        // when you hear a player is done with their action for this phase,
        // check if all players are done. If they are, then send an UpdatePhase
        // event to the server Program.
        if(e.PropertyName == "Done" && e.player.Done.Value)
        {
            foreach (Player p in PlayerList)
            {
                if(!p.Done.Value)
                {
                    return;
                }
            }
            NextPhase(); // go to the next phase
        }
    }

    private void NextPhase()
    {
        Console.WriteLine("Moving to the next phase");
        if(CurrentPhase == Phase.START)
        {
            CurrentPhase = Phase.NIGHT;
        }
        else if(CurrentPhase == Phase.NIGHT)
        {
            CurrentPhase = Phase.NOMINATE;
        }
        else if(CurrentPhase == Phase.NOMINATE)
        {
            // TODO: add logic to decide whether to go back to night in case of tie or go on to vote
            CurrentPhase = Phase.VOTE;
        }
        else if(CurrentPhase == Phase.VOTE)
        {
            CurrentPhase = Phase.END;
        }
    }

    #endregion

}
