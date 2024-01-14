using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace FourInARowBattle;

/// <summary>
/// This is a UI element which contains multiple counters, and the refill button
/// Each player should have one.
/// </summary>
public partial class TokenCounterListControl : Control
{
    /// <summary>
    /// Refill was pressed
    /// </summary>
    [Signal]
    public delegate void RefillAttemptedEventHandler();
    /// <summary>
    /// Token was selected
    /// </summary>
    /// <param name="what">What counter</param>
    /// <param name="who">Which button</param>
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterControl what, TokenCounterButton who);
    /// <summary>
    /// Token was hovered
    /// </summary>
    /// <param name="turn">What game turn</param>
    /// <param name="description">Token description</param>
    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    /// <summary>
    /// Token is no longer hovered
    /// </summary>
    /// <param name="turn">What game turn</param>
    /// <param name="description">Token description</param>
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
    //if true, refilling is locked even if refill is not locked
    //used to disable opponent refill button in remote play
    private bool _refillForceDisabled = false;
    private bool _refillLocked = false;
    private bool _refillUnlockedNextTurn = false;

    private int _currentScore = 0;
    /// <summary>
    /// The current score
    /// </summary>
    /// <value></value>
    private int CurrentScore
    {
        get => _currentScore;
        set
        {
            _currentScore = value;
            _scoreLabel.Text = $"Score: {_currentScore}";
        }
    }

    /// <summary>
    /// What turn the counter list is active on
    /// </summary>
    [ExportCategory("")]
    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        private set
        {
            _activeOnTurn = value;
            foreach(TokenCounterControl c in _counters)
                c.ActiveOnTurn = _activeOnTurn;
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

    /// <summary>
    /// Token was selected
    /// </summary>
    /// <param name="control">What counter</param>
    /// <param name="button">What button</param>
    private void OnTokenSelected(TokenCounterControl control, TokenCounterButton button)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(button);
        _lastSelection = control;
        _lastSelectionButton = button;
        EmitSignal(SignalName.TokenSelected, control, button);
    }

    /// <summary>
    /// Token was hovered
    /// </summary>
    /// <param name="turn">The counter's turn</param>
    /// <param name="description">The token description</param>
    private void OnTokenButtonHovered(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        EmitSignal(SignalName.TokenButtonHovered, (int)turn, description);
    }

    /// <summary>
    /// Token is no longer hovered
    /// </summary>
    /// <param name="turn">The counter's turn</param>
    /// <param name="description">The token description</param>
    private void OnTokenButtonStoppedHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        EmitSignal(SignalName.TokenButtonStoppedHover, (int)turn, description);
    }

    /// <summary>
    /// Refill pressed
    /// </summary>
    private void OnRefillButtonPressed()
    {
        if(_refillLocked || !AnyCanAdd()) return;
        EmitSignal(SignalName.RefillAttempted);
    }

    /// <summary>
    /// Refill hovered
    /// </summary>
    private void OnRefillButtonMouseEntered()
    {
        //hack: use the same system for token descriptions for the refill button
        EmitSignal( SignalName.TokenButtonHovered,
                    (int)ActiveOnTurn,
                    DescriptionLabel.REFILL_DESCRIPTION);
    }

    /// <summary>
    /// Refill no longer hovered
    /// </summary>
    private void OnRefillButtonMouseExited()
    {
        EmitSignal( SignalName.TokenButtonStoppedHover,
                    (int)ActiveOnTurn,
                    DescriptionLabel.REFILL_DESCRIPTION);
    }

    /// <summary>
    /// Force disable the counters and refill button.
    /// Used to disable opponent counters in remote play.
    /// </summary>
    /// <param name="disabled">Whether to force disable</param>
    public void SetCountersForceDisabled(bool disabled)
    {
        foreach(TokenCounterControl c in _counters)
        {
            c.ForceDisabled = disabled;
        }
        _refillForceDisabled = disabled;
    }

    /// <summary>
    /// Perform a refill
    /// </summary>
    /// <returns>Whether a refill was possible</returns>
    public bool DoRefill()
    {
        if(!AnyCanAdd()) return false;
        if(_refillLocked) return false;
        foreach(TokenCounterControl c in _counters) if(c.CanAdd()) c.Add(1);
        if(!_refillLocked) _refillLocked = true;
        return true;
    }

    /// <summary>
    /// Called when the turn changes
    /// </summary>
    /// <param name="to">What turn it was changed to</param>
    public void OnTurnChange(GameTurnEnum to)
    {
        //our turn
        if(to == ActiveOnTurn)
        {
            //force-select previous selection
            if( _lastSelectionButton is not null &&
                _lastSelection is not null &&
                _lastSelection.CanTake())
            {
                _lastSelection.OnSelectButtonPressed(_lastSelectionButton);
            }

            //refill button is locked. we will unlock it next turn.
            if(_refillLocked)
            {
                _refillLocked = false;
                _refillButton.Disabled = true || _refillForceDisabled;
                _refillUnlockedNextTurn = true;
            }
            //refill is not locked and we can add to some counter
            else if(AnyCanAdd())
            {
                _refillButton.Disabled = false || _refillForceDisabled;
                _refillUnlockedNextTurn = false;
            }
            //refill is not locked and we can't add to a counter
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

        //notify children
        foreach(TokenCounterControl c in _counters)
        {
            c.OnTurnChange(to);
        }
    }

    /// <summary>
    /// Score increased
    /// </summary>
    /// <param name="who">For whom</param>
    /// <param name="amount">How much</param>
    public void OnAddScore(GameTurnEnum who, int amount)
    {
        if(ActiveOnTurn == who)
            CurrentScore += amount;
    }

    /// <summary>
    /// Check whether any counters can get more tokens
    /// </summary>
    /// <returns>Whether any counters can get more tokens</returns>
    public bool AnyCanAdd()
    {
        foreach(TokenCounterControl c in _counters) if(c.CanAdd()) return true;
        return false;
    }

    /// <summary>
    /// Find counter that has a button with the given scene, or null if none exist
    /// </summary>
    /// <param name="scene">The scene</param>
    /// <returns>The counter with the scene, or null if none exist</returns>
    public TokenCounterControl? FindCounterOfScene(PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        foreach(TokenCounterControl c in _counters)
        {
            if(c.HasButtonForScene(scene))
                return c;
        }
        return null;
    }

    /// <summary>
    /// Load counter list data
    /// </summary>
    /// <param name="data">The data</param>
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

    /// <summary>
    /// Save current counter list state
    /// </summary>
    /// <returns>The state</returns>
    public TokenCounterListData SerializeTo() => new()
    {
        Score = CurrentScore,
        RefillLocked = _refillLocked,
        RefillUnlockedNextTurn = _refillUnlockedNextTurn,
        Counters = _counters.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
