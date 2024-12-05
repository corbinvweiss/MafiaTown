using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace MafiaTownServer;

public enum Phase // track the phase of the game
{ 
    START,
    NIGHT,
    VOTE,
    END
}

public class GameState  // global synchronized state of the game.
{

    #region Player State
    public List<Player> PlayerList {get; private set; } = new List<Player>();

    public void AddPlayer(Player newPlayer)
    {
        PlayerList.Add(newPlayer);
        // subscribe to changes in the player's state
        newPlayer.PropertyChanged += OnPlayerStateChanged;
    }

    private void OnPlayerStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Player player)
        {
            if (e.PropertyName == "Done")
            {
                foreach (Player p in PlayerList)
                {
                    if(!p.Done)
                    {
                        return;
                    }
                }
                NextPhase(); // go to the next phase
            }
        }
    }

    public void RemovePlayer(Player p)
    {
        PlayerList.Remove(p);
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
    public event PropertyChangedEventHandler? PropertyChanged;    // when the phase changes, trigger an event
    public string WhatHappened = "";    // describes what happened in the previous phase.
    private Phase _CurrentPhase = Phase.START;
    public Phase CurrentPhase
    {
        get => _CurrentPhase;
        set
        {
            ResetPlayersReady();
            SetField<Phase>(out _CurrentPhase, value);
        }
    }

    private void ResetPlayersReady()
    {
        foreach (Player player in PlayerList)
        {
            player.StartPhase();
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
            foreach (Player player in PlayerList)
            {
                if (player.Targeted == true && player.Healed == false)
                {
                    player.Alive = false;
                }
            }
            WhatHappened = GetNews();
            CurrentPhase = Phase.VOTE;
        }
        else if(CurrentPhase == Phase.VOTE)
        {
            CurrentPhase = Phase.END;
        }
    }

    private string GetNews()
    {
        return "Some people died, others were healed.";
    }

    protected virtual void SetField<T>(out T variable, T value, [CallerMemberName] string propertyName = "")
    {
        variable = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

}
