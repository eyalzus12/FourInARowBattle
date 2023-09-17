using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    
    [Export]
    public float PressDetectorOffset{get; set;} = 200;
    [Export]
    public Texture2D GhostTokenTexture{get; set;} = null!;

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

    private Board _board = null!;

    public List<Area2D> DropDetectors{get; set;} = new();
    public List<CollisionShape2D> DropDetectorShapes{get; set;} = new();

    private int? _dropDetectorIdx;
    public int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        private set
        {
            _dropDetectorIdx = value;
            if(value is null)
                _board.HideGhostToken();
            else
                _board.RenderGhostToken(GhostTokenTexture, TurnColor, (int)value);
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
        _board = GetNode<Board>("Board");

        SetupDropDetectors();
        SetDetectorsDisabled(false);
    }

    public void SetupDropDetectors()
    {
        DropDetectorIdx = null;
        foreach(var area in DropDetectors) area.QueueFree();
        DropDetectors.Clear();
        DropDetectorShapes.Clear();
        for(int col = 1; col <= _board.Columns; ++col)
        {
            Vector2 topMost = _board.HolePosition(0,col);
            Vector2 botMost = _board.HolePosition(_board.Rows+1,col);
            Vector2 center = (topMost + botMost)/2;
            Area2D area = new(){Monitorable = false};
            CollisionShape2D shape = new()
            {
                Shape = new RectangleShape2D(){Size = new(2*_board.SlotRadius, (botMost-topMost).Y + PressDetectorOffset)},
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
            DropDetectorIdx is not null
        )
        {
            TokenBase? t = null;
            if(mb.ButtonIndex == MouseButton.Left)
            {
                t = ResourceLoader
                    .Load<PackedScene>("res://Scenes/Token/TokenPlain/TokenPlain.tscn")
                    .Instantiate<TokenPlain>();
            }
            else if(mb.ButtonIndex == MouseButton.Right)
            {
                t = ResourceLoader
                    .Load<PackedScene>("res://Scenes/Token/TokenAnvil/TokenAnvil.tscn")
                    .Instantiate<TokenAnvil>();
            }
            if(t is not null)
            {
                t.TokenColor = TurnColor;
                if(_board.AddToken((int)DropDetectorIdx, t))
                {
                    Turn = NextTurn;
                    _board.QueueRedraw();
                    var res = _board.DecideResult();
                    if(res != GameResultEnum.None)
                        GD.Print(res);
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
                    _board.FlipVertical();
                    //show ghost token in correct position
                    _board.QueueRedraw();
                    needNewDetectors = false;
                    break;
                case Key.A:
                    _board.RotateLeft();
                    break;
                case Key.D:
                    _board.RotateRight();
                    break;
                default:
                    needGravity = false;
                    needResultCheck = false;
                    break;
            }

            if(needGravity)
            {
                _board.ApplyGravity();
            }

            if(needNewDetectors)
            {
                DropDetectorIdx = null;
                SetupDropDetectors();
                SetDetectorsDisabled(false);
            }

            if(needResultCheck)
            {
                var res = _board.DecideResult();
                if(res != GameResultEnum.None)
                    GD.Print(res);
            }
        }
    }
}
