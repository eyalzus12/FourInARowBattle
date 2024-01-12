using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace FourInARowBattle;

public partial class TokenCounterListControl : Control
{
    [Signal]
    public delegate void RefillAttemptedEventHandler();
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterControl what, TokenCounterButton who);
    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    [Signal]
    public delegate void TokenButtonStoppedHoverEventHandler(GameTurnEnum turn, string description);

    [ExportCategory("Nodes")]
    [Export]
    private Array<TokenCounterControl> _counters = new();
    [Export]
    private Label _scoreLabel = null!;
    [Export]
    private Button _refillButton = null!;

    private GameTurnEnum _activeOnTurn;
    private bool _refillForceDisabled = false;
    private bool _refillLocked = false;
    private bool _refillUnlockedNextTurn = false;

    private int _currentScore = 0;
    private int CurrentScore
    {
        get => _currentScore;
        set
        {
            _currentScore = value;
            _scoreLabel.Text = $"Score: {_currentScore}";
        }
    }

    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        private set
        {
            _activeOnTurn = value;
            if(IsInsideTree())
            {
                foreach(TokenCounterControl c in _counters)
                    c.ActiveOnTurn = _activeOnTurn;
            }
        }
    }

    private TokenCounterControl? _lastSelection = null;
    private TokenCounterButton? _lastSelectionButton = null;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_scoreLabel);
        ArgumentNullException.ThrowIfNull(_refillButton);
    }

    private void ConnectSignals()
    {
        foreach(TokenCounterControl c in _counters)
        {
            TokenCounterControl cBind = c;
            c.TokenSelected += (TokenCounterButton button) => OnTokenSelected(cBind, button);
            c.TokenButtonHovered += OnTokenButtonHovered;
            c.TokenButtonStoppedHover += OnTokenButtonStoppedHover;
        }

        _refillButton.Pressed += OnRefillButtonPressed;
        _refillButton.MouseEntered += OnRefillButtonMouseEntered;
        _refillButton.MouseExited += OnRefillButtonMouseExited;
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
        if(_refillLocked || !AnyCanAdd()) return;
        EmitSignal(SignalName.RefillAttempted);
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

    public void SetCountersForceDisabled(bool disabled)
    {
        foreach(TokenCounterControl c in _counters)
        {
            c.ForceDisabled = disabled;
        }
        _refillForceDisabled = disabled;
    }

    public bool DoRefill()
    {
        if(!AnyCanAdd()) return false;
        if(_refillLocked) return false;
        foreach(TokenCounterControl c in _counters) if(c.CanAdd()) c.Add(1);
        if(!_refillLocked) _refillLocked = true;
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
                _refillButton.Disabled = true || _refillForceDisabled;
                _refillUnlockedNextTurn = true;
            }
            else if(AnyCanAdd())
            {
                _refillButton.Disabled = false || _refillForceDisabled;
                _refillUnlockedNextTurn = false;
            }
            else
            {
                _refillButton.Disabled = true || _refillForceDisabled;
                _refillUnlockedNextTurn = false;
            }
        }
        //opponent's turn
        else
        {
            _refillButton.Disabled = true || _refillForceDisabled;
        }

        foreach(TokenCounterControl c in _counters)
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
        foreach(TokenCounterControl c in _counters) if(c.CanAdd()) return true;
        return false;
    }

    public TokenCounterControl? FindCounterOfScene(PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        foreach(TokenCounterControl c in _counters)
        {
            foreach(TokenCounterButton b in c.TokenButtons)
            {
                if(b.AssociatedScene == scene)
                {
                    return c;
                }
            }
        }
        return null;
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

        if(data.Counters.Count != _counters.Count)
        {
            GD.PushError($"Token counter list has {_counters.Count} counters, and there was an attempt to create it from data with {data.Counters.Count} counters");
            return;
        }
        for(int i = 0; i < _counters.Count; ++i)
            _counters[i].DeserializeFrom(data.Counters[i]);
    }

    public TokenCounterListData SerializeTo() => new()
    {
        Score = CurrentScore,
        RefillLocked = _refillLocked,
        RefillUnlockedNextTurn = _refillUnlockedNextTurn,
        Counters = _counters.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
