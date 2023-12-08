using Godot;

namespace FourInARowBattle;

public partial class TokenCounterControl : Control
{
    private Texture2D _texture = null!;
    private int _count = 0;

    [Export]
    public Godot.Collections.Array<TokenCounterButton> TokenButtons{get; set;} = null!;

    [Export]
    public Label TokenCountLabel{get; set;} = null!;

    [Export]
    public bool Infinite{get; set;} = false;
    [Export]
    public int TokenMaxCount{get; set;} = 5;

    [Export]
    public int TokenCount
    {
        get => _count;
        set
        {
            _count = value;
            if(IsInsideTree())
            {
                TokenCountLabel.Text = Infinite?"∞/∞":$"{value}/{TokenMaxCount}";
                if(!CanTake())
                    Disabled = true;
            }
        }
    }

    private bool _disabled = false;
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            foreach(TokenCounterButton button in TokenButtons)
                button.Disabled = _disabled;
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
            if(IsInsideTree())
            {
                foreach(TokenCounterButton button in TokenButtons)
                {
                    button.Modulate = _activeOnTurn.GameTurnToColor();
                }
            }
        }
    }

    private EventBus _eventBus = null!;

    public override void _Ready()
    {
        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));
        
        TokenCount = _count;
        ActiveOnTurn = _activeOnTurn;

        foreach(TokenCounterButton button in TokenButtons)
        {
            TokenCounterButton buttonBind = button;
            button.Pressed += () =>
                OnSelectButtonPressed(buttonBind);
            button.MouseEntered += () =>
                _eventBus.EmitSignal(
                    EventBus.SignalName.TokenButtonHovered,
                    (int)ActiveOnTurn,
                    DescriptionLabel.DescriptionFromScene(buttonBind.AssociatedScene)
                );
            button.MouseExited += () =>
                _eventBus.EmitSignal(
                    EventBus.SignalName.TokenButtonStoppedHover,
                    (int)ActiveOnTurn,
                    DescriptionLabel.DescriptionFromScene(buttonBind.AssociatedScene)
                );
        }
            
        _eventBus.TurnChanged += OnTurnChange;
    }

    public void OnSelectButtonPressed(TokenCounterButton who)
    {
        _eventBus.EmitSignal(EventBus.SignalName.TokenSelected, this, who);
    }

    public void OnTurnChange(GameTurnEnum to, bool isStartupSignal)
    {
        Disabled = to != ActiveOnTurn || !CanTake();
    }

    public bool CanTake() => Infinite || (TokenCount > 0);
    public bool CanAdd() => !Infinite && (TokenCount < TokenMaxCount);
    public void Take(int amount){if(!Infinite) TokenCount -= amount;}
    public void Add(int amount){if(!Infinite) TokenCount += amount;}

    public void DeserializeFrom(TokenCounterData data)
    {
        TokenCount = data.TokenCount;
    }

    public TokenCounterData SerializeTo() => new()
    {
        TokenCount = TokenCount
    };
}
