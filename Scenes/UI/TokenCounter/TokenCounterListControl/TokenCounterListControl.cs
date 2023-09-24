using Godot;
using System;

public partial class TokenCounterListControl : Control
{
    private GameTurnEnum _activeOnTurn;

    [Export]
    public Godot.Collections.Array<TokenCounterControl> Counters{get; set;} = new();

    [Export]
    public Label ScoreLabel{get; set;} = null!;

    [Export]
    public Button RefillButton{get; set;} = null!;

    private bool _refillLocked = false;
    private bool _noneCanAdd = true;

    private int _currentScore = 0;
    public int CurrentScore
    {
        get => _currentScore;
        set
        {
            _currentScore = value;
            if(ScoreLabel is not null)
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
                foreach(var c in Counters)
                    c.ActiveOnTurn = _activeOnTurn;
            }
        }
    }

    private TokenCounterControl? _lastSelection = null;
    private TokenCounterButton? _lastSelectionButton = null;
    private EventBus _eventBus = null!;

    public override void _Ready()
    {
        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));

        ActiveOnTurn = _activeOnTurn;
        CurrentScore = 0;
        foreach(var c in Counters)
        {
            var cBind = c;
            foreach(var button in c.TokenButtons)
            {
                var bBind = button;
                button.Pressed += () =>
                {
                    _lastSelection = cBind;
                    _lastSelectionButton = bBind;
                };
            }
        }
        RefillButton.Pressed += () =>
        {
            if(!AnyCanAdd()) return;
            foreach(var c in Counters) if(c.CanAdd()) c.Add(1);
            _eventBus.EmitSignal(EventBus.SignalName.ExternalPassTurn);
            if(!_refillLocked) _refillLocked = true;
        };
        _eventBus.TurnChanged += OnTurnChange;
        _eventBus.ScoreIncreased += OnAddScore;
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
            }
            else if(AnyCanAdd())
                RefillButton.Disabled = false;
            else
                RefillButton.Disabled = true;
        }
        //opponent's turn
        else
        {
            RefillButton.Disabled = true;
        }
    }

    public void OnAddScore(GameTurnEnum who, int amount)
    {
        if(ActiveOnTurn == who)
            CurrentScore += amount;
    }

    public bool AnyCanAdd()
    {
        foreach(var c in Counters) if(c.CanAdd()) return true;
        return false;
    }
}
