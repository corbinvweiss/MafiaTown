using System;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace MafiaTownServer;

public enum Phase // track the phase of the game
{ 
    PREGAME,
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

public class Player
{
    // Event triggered whenever the player state changes
    public event Action<Player, string>? PlayerStateChanged;
    public void OnPlayerStateChanged(string propertyName)
    {
        PlayerStateChanged?.Invoke(this, propertyName);
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
}

public class GameState  // global synchronized state of the game.
{
    public object StateLock { get; } = new object();    // lock to synchronize access to GameState

    #region Player State
    public event Action<Player>? PlayerAdded;
    public event Action<Player>? PlayerRemoved;
    public List<Player> PlayerList {get; private set; } = new List<Player>();

    

    public void AddPlayer(Player newPlayer)
    {
        lock (StateLock)
        {
            PlayerList.Add(newPlayer);
        }
        // subscribe to changes in the player's state
        newPlayer.PlayerStateChanged += OnPlayerStateChanged;
        PlayerAdded?.Invoke(newPlayer);
    }

    public void RemovePlayer(Player p)
    {
        lock (StateLock)
        {
            PlayerList.Remove(p);
        }
        PlayerRemoved?.Invoke(p);
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
    public event Action<Phase>? PhaseChanged;    // when the phase changes, trigger an event
    private Phase currentPhase = Phase.PREGAME;

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
                }
                else
                {
                    return; // no change, so no event
                }
            }
            PhaseChanged?.Invoke(notifyPhase);  // fire PhaseChanged event to notify clients of new phase.
        }
    }

    private void OnPlayerStateChanged(Player player, string propertyName)
    {
        // when you hear a player is done with their action for this phase,
        // check if all players are done. If they are, then send an UpdatePhase
        // event to the server Program.
        if(propertyName == "Done" && player.Done.Value)
        {
            bool allDone = true;
            foreach (Player p in PlayerList)
            {
                if(!p.Done.Value)
                {
                    allDone = false;
                }
            }
            if(allDone) {
                NextPhase(); // go to the next phase
            }
        }
    }

    private void NextPhase()
    {
        Console.WriteLine("Moving to the next phase");
        if(CurrentPhase == Phase.PREGAME)
        {
            CurrentPhase = Phase.START;
        }
        else if(CurrentPhase == Phase.START)
        {
            CurrentPhase = Phase.NIGHT;
        }
    }

    #endregion

}
