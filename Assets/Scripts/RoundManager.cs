using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoundManager : NetworkBehaviour
{
    public enum RoundType
    {
        Extended, 
        Secret,
        Traitor,
        NukeOps,
        Wizard
    }

    public struct Round
    {
        public long StartTime;
        public long EndTime;
        public bool Active;
        public string Name;
        public string Category;
        
        public string GetDisplayName(bool isAdmin = false)
        {
            if (string.Empty.Equals(Category))
            {
                return Name;
            }

            return isAdmin ? Category + " (" + Name + ")" : Category;
        }

        public string GetElapsedTimeDisplay()
        {
            return $"{(Active ? DateTime.Now : new DateTime(EndTime)) - new DateTime(StartTime):c}";
        }
    }

    [SyncVar(hook = "SyncCurrentRound")]
    private Round _currentRound = new Round
    {
        Name = "None"
    };
    
    [SyncVar(hook = "SyncOnDeckRound")]
    private Round _onDeckRound;

    private void SyncCurrentRound(Round round)
    {
        _currentRound = round;
    }
    
    private void SyncOnDeckRound(Round round)
    {
        _onDeckRound = round;
    }

    public override void OnStartServer()
    {
        SyncCurrentRound(this._currentRound);
        SyncOnDeckRound(this._onDeckRound);
        
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        SyncCurrentRound(this._currentRound);
        SyncOnDeckRound(this._onDeckRound);
        
        base.OnStartClient();
    }

    public Round GetCurrentRound()
    {
        return _currentRound;
    }

    public void StartNextRound()
    {
        EndRound();

        _onDeckRound.Active = true;
        _onDeckRound.StartTime = DateTime.Now.Ticks;
        
        SyncCurrentRound(_onDeckRound);
    }

    public void EndRound()
    {
        if (_currentRound.Active)
        {
            var round = _currentRound;
            round.Active = false;
            round.EndTime = DateTime.Now.Ticks;

            SyncCurrentRound(round);
        }
    }
    
    public void SetNextRound(RoundType roundType = RoundType.Extended)
    {
        switch (roundType)
        {
            case RoundType.Extended:
                _onDeckRound = new Round
                {
                    Name = "Extended"
                };
                break;
            case RoundType.Secret:
                SetNextRound(SelectRoundFromList(new List<KeyValuePair<RoundType, double>> {
                    new KeyValuePair<RoundType, double>(RoundType.Wizard, 0.1), // 10%
                    new KeyValuePair<RoundType, double>(RoundType.NukeOps, 0.3), // 20%
                    new KeyValuePair<RoundType, double>(RoundType.Traitor, 1.0) // 70%
                }));
                _onDeckRound.Category = "Secret";
                break;
            case RoundType.Traitor:
                _onDeckRound = new Round
                {
                    Name = "Traitor"
                };
                break;
            case RoundType.NukeOps:
                _onDeckRound = new Round
                {
                    Name = "Nuke Ops"
                };
                break;
            case RoundType.Wizard:
                _onDeckRound = new Round
                {
                    Name = "Wizard"
                };
                break;
        }
    }

    private RoundType SelectRoundFromList(List<KeyValuePair<RoundType, double>> rounds)
    {
        var diceRoll = Random.Range(0.0f, 1.0f);
        var cumulative = 0.0;
        for (var i = 0; i < rounds.Count; i++)
        {
            cumulative += rounds[i].Value;
            if (diceRoll < cumulative)
            {
                return rounds[i].Key;
            }
        }

        return RoundType.Extended;
    }
}


