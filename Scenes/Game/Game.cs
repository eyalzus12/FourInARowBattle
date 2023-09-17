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

    private Board _board = null!;

    public List<Area2D> DropDetectors{get; set;} = new();
    public List<CollisionShape2D> DropDetectorShapes{get; set;} = new();
    public int? DropDetectorIdx{get; private set;}

    public void OnDropDetectorMouseEnter(int col) => DropDetectorIdx = col;
    public void OnDropDetectorMouseExit(int col) => DropDetectorIdx = (DropDetectorIdx == col)?null:DropDetectorIdx;

    public override void _Ready()
    {
        _board = GetNode<Board>("Board");

        SetupDropDetectors();
        SetDetectorsDisabled(false);
        GetTree().CreateTimer(5).Timeout += () =>
        {
            _board.RotateLeft();
            SetupDropDetectors();
            SetDetectorsDisabled(false);
        };
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
}
