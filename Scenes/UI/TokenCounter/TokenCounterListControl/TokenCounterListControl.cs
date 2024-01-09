using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace FourInARowBattle;

public partial class TokenCounterListControl : Control
{
    [Signal]
    public delegate void RefilledTokensEventHandler();
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterControl what, TokenCounterButton who);
    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    [Signal]
    public delegate void TokenButtonStoppedHoverEventHandler(GameTurnEnum turn, string description);

    private GameTurnEnum _activeOnTurn;

    [Export]
    public Array<TokenCounterControl> Counters{get; set;} = new();

    [Export]
    public Label ScoreLabel{get; set;} = null!;

    [Export]
    public Button RefillButton{get; set;} = null!;

    private bool _refillLocked = false;
    private bool _refillUnlockedNextTurn = false;

    private int _currentScore = 0;
    public int CurrentScore
    {
        get => _currentScore;
        set
        {
            _currentScore = value;
            ScoreLabel.Text = $"Score: {_currentScore}";
        }
    }

    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        set
        {
            _activeOnTurn = value;
            if(IsInsideTree())
            {
                foreach(TokenCounterControl c in Counters)
                    c.ActiveOnTurn = _activeOnTurn;
            }
        }
    }

    private TokenCounterControl? _lastSelection = null;
    private TokenCounterButton? _lastSelectionButton = null;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(ScoreLabel);
        ArgumentNullException.ThrowIfNull(RefillButton);
    }

    private void ConnectSignals()
    {
        foreach(TokenCounterControl c in Counters)
        {
            TokenCounterControl cBind = c;
            c.TokenSelected += (TokenCounterButton button) => OnTokenSelected(cBind, button);
            c.TokenButtonHovered += OnTokenButtonHovered;
            c.TokenButtonStoppedHover += OnTokenButtonStoppedHover;
        }

        RefillButton.Pressed += OnRefillButtonPressed;
        RefillButton.MouseEntered += OnRefillButtonMouseEntered;
        RefillButton.MouseExited += OnRefillButtonMouseExited;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        ActiveOnTurn = _activeOnTurn;
        CurrentScore = 0;
    }

    private void OnTokenSelected(TokenCounterControl control, TokenCounterButton button)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(button);
        _lastSelection = control;
        _lastSelectionButton = button;
        EmitSignal(SignalName.TokenSelected, control, button);
    }

    private void OnTokenButtonHovered(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        EmitSignal(SignalName.TokenButtonHovered, (int)turn, description);
    }

    private void OnTokenButtonStoppedHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        EmitSignal(SignalName.TokenButtonStoppedHover, (int)turn, description);
    }

    private void OnRefillButtonPressed()
    {
        DoRefill();
    }

    private void OnRefillButtonMouseEntered()
    {
        EmitSignal(
            SignalName.TokenButtonHovered,
            (int)ActiveOnTurn,
            DescriptionLabel.REFILL_DESCRIPTION
        );
    }

    private void OnRefillButtonMouseExited()
    {
        EmitSignal(
            SignalName.TokenButtonStoppedHover, 
            (int)ActiveOnTurn,
            DescriptionLabel.REFILL_DESCRIPTION
        );
    }

    public bool DoRefill()
    {
        if(_refillLocked || !AnyCanAdd()) return false;
        foreach(TokenCounterControl c in Counters) if(c.CanAdd()) c.Add(1);
        if(!_refillLocked) _refillLocked = true;
        EmitSignal(SignalName.RefilledTokens);
        return true;
    }

    public void OnTurnChange(GameTurnEnum to)
    {
        //our turn
        if(to == ActiveOnTurn)
        {
            //force-select previous selection
            if(_lastSelectionButton is not null && 
                _lastSelection is not null &&
                _lastSelection.CanTake()
            )
                _lastSelection.OnSelectButtonPressed(_lastSelectionButton);
            //lock refill button if needed
            if(_refillLocked)
            {
                _refillLocked = false;
                RefillButton.Disabled = true;
                _refillUnlockedNextTurn = true;
            }
            else if(AnyCanAdd())
            {
                RefillButton.Disabled = false;
                _refillUnlockedNextTurn = false;
            }
            else
            {
                RefillButton.Disabled = true;
                _refillUnlockedNextTurn = false;
            }
        }
        //opponent's turn
        else
        {
            RefillButton.Disabled = true;
        }

        foreach(TokenCounterControl c in Counters)
        {
            c.OnTurnChange(to);
        }
    }

    public void OnAddScore(GameTurnEnum who, int amount)
    {
        if(ActiveOnTurn == who)
            CurrentScore += amount;
    }

    public bool AnyCanAdd()
    {
        foreach(TokenCounterControl c in Counters) if(c.CanAdd()) return true;
        return false;
    }

    public void DeserializeFrom(TokenCounterListData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _lastSelection = null;
        _lastSelectionButton = null;
        
        CurrentScore = data.Score;
        _refillLocked = data.RefillLocked;
        _refillUnlockedNextTurn = data.RefillUnlockedNextTurn;
        //a bit of a heck: we'd activate the turn change signal after restoring
        //but this can cause the refill button to incorrectly be re-activated
        //so this assignment is needed to correctly get the previous state
        if(_refillUnlockedNextTurn) _refillLocked = true;

        if(data.Counters.Count != Counters.Count)
        {
            GD.PushError($"Token counter list has {Counters.Count} counters, and there was an attempt to create it from data with {data.Counters.Count} counters");
            return;
        }
        for(int i = 0; i < Counters.Count; ++i)
            Counters[i].DeserializeFrom(data.Counters[i]);
    }

    public TokenCounterListData SerializeTo() => new()
    {
        Score = CurrentScore,
        RefillLocked = _refillLocked,
        RefillUnlockedNextTurn = _refillUnlockedNextTurn,
        Counters = Counters.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
