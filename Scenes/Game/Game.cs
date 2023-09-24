using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    
    [Export]
    public float PressDetectorOffset{get; set;} = 200;
    
    private TokenCounterControl? _selectedControl = null;

    public GameTurnEnum Turn{get; set;} = GameTurnEnum.Player1;
    public Color TurnColor => Turn switch
    {
        GameTurnEnum.Player1 => Colors.Red,
        GameTurnEnum.Player2 => Colors.Blue,
        _ => Colors.White
    };

    public GameTurnEnum NextTurn => Turn switch
    {
        GameTurnEnum.Player1 => GameTurnEnum.Player2,
        GameTurnEnum.Player2 => GameTurnEnum.Player1,
        _ => throw new ArgumentException($"Invalid turn {Turn}")
    };

    [Export]
    public Board GameBoard{get; set;} = null!;

    public List<Area2D> DropDetectors{get; set;} = new();
    public List<CollisionShape2D> DropDetectorShapes{get; set;} = new();

    private EventBus _eventBus = null!;

    private int? _dropDetectorIdx;
    public int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        private set
        {
            _dropDetectorIdx = value;
            if(value is null)
                GameBoard.HideGhostToken();
            else if(_selectedControl is not null && _selectedControl.CanTake())
                GameBoard.RenderGhostToken(_selectedControl.TokenTexture, TurnColor, (int)value);
        }
    }

    public void OnDropDetectorMouseEnter(int col)
    {
        DropDetectorIdx = col;
    }
    public void OnDropDetectorMouseExit(int col)
    {
        if(DropDetectorIdx == col)
            DropDetectorIdx = null;
    }

    public override void _Ready()
    {
        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));

        SetupDropDetectors();
        SetDetectorsDisabled(false);
        _eventBus.TokenSelected += OnTokenSelected;
        _eventBus.ExternalPassTurn += PassTurn;

        //_Ready is called on children before the parent
        //so we can do this to signal the token counters
        //and update their disabled/enabled state
        _eventBus.EmitSignal(EventBus.SignalName.TurnChanged, (int)Turn);
    }

    public void SetupDropDetectors()
    {
        DropDetectorIdx = null;
        foreach(var area in DropDetectors) area.QueueFree();
        DropDetectors.Clear();
        DropDetectorShapes.Clear();
        for(int col = 1; col <= GameBoard.Columns; ++col)
        {
            Vector2 topMost = GameBoard.HolePosition(0,col);
            Vector2 botMost = GameBoard.HolePosition(GameBoard.Rows+1,col);
            Vector2 center = (topMost + botMost)/2;
            Area2D area = new(){Monitorable = false};
            CollisionShape2D shape = new()
            {
                Shape = new RectangleShape2D(){Size = new(2*GameBoard.SlotRadius, (botMost-topMost).Y + PressDetectorOffset)},
                Disabled = true
            };
            area.AddChild(shape);
            var colBind = col-1;
            area.MouseExited += () => OnDropDetectorMouseExit(colBind);
            area.MouseEntered += () => OnDropDetectorMouseEnter(colBind);
            DropDetectorShapes.Add(shape);
            DropDetectors.Add(area);
            AddChild(area);
            area.GlobalPosition = center;
        }
    }

    public void SetDetectorsDisabled(bool disabled)
    {
        DropDetectorIdx = null;
        foreach(var col in DropDetectorShapes)
            col.SetDeferred(CollisionShape2D.PropertyName.Disabled, disabled);
    }

    public override void _Input(InputEvent @event)
    {
        if(
            @event.IsPressed() && !@event.IsEcho() && 
            @event is InputEventMouseButton mb &&
            DropDetectorIdx is not null &&
            _selectedControl is not null &&
            _selectedControl.CanTake()
        )
        {
            if(mb.ButtonIndex == MouseButton.Left)
            {
                TokenBase t = _selectedControl.AssociatedScene.Instantiate<TokenBase>();
                t.TokenColor = TurnColor;
                if(GameBoard.AddToken((int)DropDetectorIdx, t))
                {
                    _selectedControl.Take(1);
                    PassTurn();

                    /*Tween? tween = t.GetMetaOrNull(Board.TOKEN_TWEEN_META_NAME)?.As<Tween>();
                    if(!IsInstanceValid(tween)) tween = null;
                    
                    if(tween is not null)
                        tween.Finished += DecideResult;
                    else
                        DecideResult();
                    
                    void DecideResult()
                    {
                        var res = GameBoard.DecideResult();
                        if(res != GameResultEnum.None)
                            GD.Print(res);
                    }*/
                }
            }
        }

        if(
            @event.IsPressed() && !@event.IsEcho() &&
            @event is InputEventKey ek
        )
        {
            bool needGravity = true;
            bool needNewDetectors = true;
            bool needResultCheck = true;
            switch(ek.Keycode)
            {
                case Key.W or Key.S:
                    GameBoard.FlipVertical();
                    //show ghost token in correct position
                    GameBoard.QueueRedraw();
                    needNewDetectors = false;
                    break;
                case Key.A:
                    GameBoard.RotateLeft();
                    break;
                case Key.D:
                    GameBoard.RotateRight();
                    break;
                default:
                    needGravity = false;
                    needResultCheck = false;
                    break;
            }

            if(needGravity)
            {
                GameBoard.ApplyGravity();
            }

            if(needNewDetectors)
            {
                DropDetectorIdx = null;
                SetupDropDetectors();
                SetDetectorsDisabled(false);
            }

            /*if(needResultCheck)
            {
                var res = GameBoard.DecideResult();
                if(res != GameResultEnum.None)
                    GD.Print(res);
            }*/
        }
    }

    public void PassTurn()
    {
        Turn = NextTurn;
        _selectedControl = null;
        _eventBus.EmitSignal(EventBus.SignalName.TurnChanged, (int)Turn);
    }

    public void OnTokenSelected(TokenCounterControl what)
    {
        if(what.ActiveOnTurn != Turn || !what.CanTake()) return;
        _selectedControl = what;
        if(DropDetectorIdx is not null)
            GameBoard.RenderGhostToken(_selectedControl.TokenTexture, TurnColor, (int)DropDetectorIdx);
    }
}
