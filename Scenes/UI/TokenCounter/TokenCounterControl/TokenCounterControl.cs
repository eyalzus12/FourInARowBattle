using Godot;
using System;

public partial class TokenCounterControl : Control
{
    [Signal]
    public delegate void CountChangedEventHandler();

    private Texture2D _texture = null!;
    private int _count = 0;


    [Export]
    public TextureRect TokenTextureRect{get; set;} = null!;

    [Export]
    public Label TokenCountLabel{get; set;} = null!;

    [Export]
    public Button TokenSelectButton{get; set;} = null!;

    [Export]
    public Texture2D TokenTexture
    {
        get => _texture;
        set
        {
            _texture = value;
            if(IsInsideTree()) TokenTextureRect.Texture = _texture;
        }
    }

    [Export]
    public bool Infinite{get; set;} = false;
    [Export]
    public int TokenMaxCount{get; set;} = 10;

    [Export]
    public int TokenCount
    {
        get => _count;
        set
        {
            _count = value;
            if(IsInsideTree())
            {
                EmitSignal(SignalName.CountChanged);
                TokenCountLabel.Text = Infinite?"∞/∞":$"{value}/{TokenMaxCount}";
                if(!CanTake()) TokenSelectButton.Disabled = true;
            }
        }
    }

    [Export]
    public PackedScene AssociatedScene{get; set;} = null!;

    private GameTurnEnum _activeOnTurn;

    [Export]
    public GameTurnEnum ActiveOnTurn
    {
        get => _activeOnTurn;
        set
        {
            _activeOnTurn = value;
            if(IsInsideTree())
                TokenTextureRect.Modulate = _activeOnTurn switch
                {
                    GameTurnEnum.Player1 => Colors.Red,
                    GameTurnEnum.Player2 => Colors.Blue,
                    _ => Colors.White
                };
        }
    }

    private EventBus _eventBus = null!;

    public override void _Ready()
    {
        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));
        
        TokenTexture = _texture;
        TokenCount = _count;
        ActiveOnTurn = _activeOnTurn;

        TokenSelectButton.Pressed += OnSelectButtonPressed;
        _eventBus.TurnChanged += OnTurnChange;
    }

    public void OnSelectButtonPressed()
    {
        _eventBus.EmitSignal(EventBus.SignalName.TokenSelected, this);
    }

    public void OnTurnChange(GameTurnEnum to)
    {
        TokenSelectButton.Disabled = to != ActiveOnTurn && CanTake();
    }

    public bool CanTake() => Infinite || (TokenCount > 0);
    public bool CanAdd() => !Infinite && (TokenCount < TokenMaxCount);
    public void Take(int amount){if(!Infinite) TokenCount -= amount;}
    public void Add(int amount){if(!Infinite) TokenCount += amount;}
}
