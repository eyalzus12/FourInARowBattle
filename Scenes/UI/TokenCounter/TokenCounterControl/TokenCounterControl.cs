using System;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// UI element that contains multiple token buttons, and counts how many tokens there are.
/// </summary>
public partial class TokenCounterControl : Control
{
    /// <summary>
    /// A token has been selected
    /// </summary>
    /// <param name="who">Which button</param>
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterButton who);
    /// <summary>
    /// A token has been hovered
    /// </summary>
    /// <param name="turn">The game turn of the button</param>
    /// <param name="description">The description of the token</param>
    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    /// <summary>
    /// A token is no longer hovered
    /// </summary>
    /// <param name="turn">The game turn of the button</param>
    /// <param name="description">The description of the token</param>
    [Signal]
    public delegate void TokenButtonStoppedHoverEventHandler(GameTurnEnum turn, string description);

    private int _count = 0;

    [ExportCategory("Nodes")]
    [Export]
    private Godot.Collections.Array<TokenCounterButton> _tokenButtons = null!;
    [Export]
    private Label _tokenCountLabel = null!;

    /// <summary>
    /// Are the tokens in infinite supply
    /// </summary>
    [ExportCategory("")]
    [Export]
    private bool _infinite = false;
    /// <summary>
    /// Max amount of tokens that this counter can hold. Does not apply if _infinite is true.
    /// </summary>
    [Export]
    private int _tokenMaxCount = 5;

    /// <summary>
    /// How many tokens are current usable. Does not apply if _infinite is true.
    /// </summary>
    [Export]
    private int TokenCount
    {
        get => _count;
        set
        {
            _count = value;
            if(IsInsideTree())
            {
                _tokenCountLabel.Text = _infinite?"∞/∞":$"{value}/{_tokenMaxCount}";
                if(!CanTake())
                    Disabled = true;
            }
        }
    }

    private bool _disabled = false;
    /// <summary>
    /// Whether the counter is current disabled
    /// </summary>
    private bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            foreach(TokenCounterButton button in _tokenButtons)
                button.Disabled = _disabled || _forceDisabled;
        }
    }

    private bool _forceDisabled = false;
    /// <summary>
    /// Whether the counter will be disabled even if Disabled is false
    /// </summary>
    public bool ForceDisabled
    {
        get => _forceDisabled;
        set
        {
            _forceDisabled = value;
            foreach(TokenCounterButton button in _tokenButtons)
                button.Disabled = _disabled || _forceDisabled;
        }
    }

    private GameTurnEnum _activeOnTurn;

    /// <summary>
    /// Which turn the counter is active on
    /// </summary>
    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        set
        {
            _activeOnTurn = value;
            foreach(TokenCounterButton button in _tokenButtons)
            {
                button.Modulate = _activeOnTurn.GameTurnToColor();
            }
        }
    }

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_tokenCountLabel);
    }

    private void ConnectSignals()
    {
        foreach(TokenCounterButton button in _tokenButtons)
        {
            TokenCounterButton buttonBind = button;
            button.Pressed += () => OnSelectButtonPressed(buttonBind);
            button.MouseEntered += () => OnTokenCounterButtonMouseEntered(buttonBind);
            button.MouseExited += () => OnTokenCounterButtonMouseExited(buttonBind);
        }
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();

        TokenCount = _count;
        ActiveOnTurn = _activeOnTurn;
    }

    /// <summary>
    /// Token was selected
    /// </summary>
    /// <param name="who">What button</param>
    public void OnSelectButtonPressed(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal(SignalName.TokenSelected, who);
    }

    /// <summary>
    /// Token was hovered
    /// </summary>
    /// <param name="who">What button</param>
    private void OnTokenCounterButtonMouseEntered(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal( SignalName.TokenButtonHovered,
                    (int)ActiveOnTurn,
                    DescriptionLabel.DescriptionFromScene(who.AssociatedScene));
    }

    /// <summary>
    /// Token is no longer hovered
    /// </summary>
    /// <param name="who">What button</param>
    private void OnTokenCounterButtonMouseExited(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal( SignalName.TokenButtonStoppedHover,
                    (int)ActiveOnTurn,
                    DescriptionLabel.DescriptionFromScene(who.AssociatedScene));
    }

    /// <summary>
    /// Turn was changed
    /// </summary>
    /// <param name="to">What turn it was changed to</param>
    public void OnTurnChange(GameTurnEnum to)
    {
        Disabled = to != ActiveOnTurn || !CanTake();
    }

    /// <summary>
    /// Whether it is possible to take a token out
    /// </summary>
    public bool CanTake() => _infinite || (TokenCount > 0);
    /// <summary>
    /// Whether it is posssible to add a token
    /// </summary>
    public bool CanAdd() => !_infinite && (TokenCount < _tokenMaxCount);
    /// <summary>
    /// Whether it is possible to take that amount out
    /// </summary>
    /// <param name="amount">The amount</param>
    public void Take(int amount) { if(!_infinite) TokenCount -= amount; }
    /// <summary>
    /// Whether it is possible to add that amount
    /// </summary>
    /// <param name="amount">The amount</param>
    public void Add(int amount) { if(!_infinite) TokenCount += amount; }

    /// <summary>
    /// Check whether there is a button that has a specific scene attached
    /// </summary>
    /// <param name="scene">The scene</param>
    /// <returns>Whether such a button exists</returns>
    public bool HasButtonForScene(PackedScene scene)
    {
        foreach(TokenCounterButton b in _tokenButtons)
        {
            if(b.AssociatedScene == scene)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Load counter data
    /// </summary>
    /// <param name="data">The data</param>
    public void DeserializeFrom(TokenCounterData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        TokenCount = data.TokenCount;
    }

    /// <summary>
    /// Save current counter state
    /// </summary>
    /// <returns>The state</returns>
    public TokenCounterData SerializeTo() => new()
    {
        TokenCount = TokenCount
    };
}
