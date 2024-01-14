using System;
using Godot;

namespace FourInARowBattle;

public partial class TokenCounterControl : Control
{
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterButton who);
    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    [Signal]
    public delegate void TokenButtonStoppedHoverEventHandler(GameTurnEnum turn, string description);

    private int _count = 0;

    [Export]
    public Godot.Collections.Array<TokenCounterButton> TokenButtons{get; private set;} = null!;

    [Export]
    private Label _tokenCountLabel = null!;

    [Export]
    private bool _infinite = false;
    [Export]
    private int _tokenMaxCount = 5;

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
    private bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            foreach(TokenCounterButton button in TokenButtons)
                button.Disabled = _disabled || _forceDisabled;
        }
    }

    private bool _forceDisabled = false;
    public bool ForceDisabled
    {
        get => _forceDisabled;
        set
        {
            _forceDisabled = value;
            foreach(TokenCounterButton button in TokenButtons)
                button.Disabled = _disabled || _forceDisabled;
        }
    }

    private GameTurnEnum _activeOnTurn;

    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        set
        {
            _activeOnTurn = value;
            foreach(TokenCounterButton button in TokenButtons)
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
        foreach(TokenCounterButton button in TokenButtons)
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
    
    public void OnSelectButtonPressed(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal(SignalName.TokenSelected, who);
    }

    private void OnTokenCounterButtonMouseEntered(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal(
            SignalName.TokenButtonHovered,
            (int)ActiveOnTurn,
            DescriptionLabel.DescriptionFromScene(who.AssociatedScene)
        );
    }

    private void OnTokenCounterButtonMouseExited(TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(who);
        EmitSignal(
            SignalName.TokenButtonStoppedHover,
            (int)ActiveOnTurn,
            DescriptionLabel.DescriptionFromScene(who.AssociatedScene)
        );
    }

    public void OnTurnChange(GameTurnEnum to)
    {
        Disabled = to != ActiveOnTurn || !CanTake();
    }

    public bool CanTake() => _infinite || (TokenCount > 0);
    public bool CanAdd() => !_infinite && (TokenCount < _tokenMaxCount);
    public void Take(int amount){if(!_infinite) TokenCount -= amount;}
    public void Add(int amount){if(!_infinite) TokenCount += amount;}

    public void DeserializeFrom(TokenCounterData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        TokenCount = data.TokenCount;
    }

    public TokenCounterData SerializeTo() => new()
    {
        TokenCount = TokenCount
    };
}
