using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

public partial class Game : Node2D
{
    
    [Export]
    public float PressDetectorOffset{get; set;} = 200;
    
    private TokenCounterControl? _selectedControl = null;
    private TokenCounterButton? _selectedButton = null;

    public GameTurnEnum Turn{get; set;} = GameTurnEnum.Player1;
    public Color TurnColor => Turn.GameTurnToColor();

    public GameTurnEnum NextTurn => Turn switch
    {
        GameTurnEnum.Player1 => GameTurnEnum.Player2,
        GameTurnEnum.Player2 => GameTurnEnum.Player1,
        _ => throw new ArgumentException($"Invalid turn {Turn}")
    };

    [Export]
    public Board GameBoard{get; set;} = null!;

    [Export]
    public Godot.Collections.Array<TokenCounterListControl> CounterLists{get; set;} = new();

    public List<Area2D> DropDetectors{get; set;} = new();
    public List<CollisionShape2D> DropDetectorShapes{get; set;} = new();

    private int? _dropDetectorIdx;
    public int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        private set
        {
            _dropDetectorIdx = value;
            if(
                value is not null &&
                _selectedControl is not null &&
                _selectedControl.CanTake() &&
                _selectedButton is not null
            )
                GameBoard.RenderGhostToken(_selectedButton.Icon, TurnColor, (int)value);
            else
                GameBoard.HideGhostToken();
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
        SetupDropDetectors();
        SetDetectorsDisabled(false);
        Autoloads.EventBus.TokenSelected += OnTokenSelected;
        foreach(TokenCounterListControl clist in CounterLists)
            clist.RefilledTokens += PassTurn;

        //_Ready is called on children before the parent
        //so we can do this to signal the token counters
        //and update their disabled/enabled state
        Autoloads.EventBus.EmitSignal(EventBus.SignalName.TurnChanged, (int)Turn, true);

        if(Autoloads.PersistentData.ContinueFromState is not null)
        {
            DeserializeFrom(Autoloads.PersistentData.ContinueFromState);
            Autoloads.PersistentData.ContinueFromState = null;
        }
    }

    public void SetupDropDetectors()
    {
        DropDetectorIdx = null;
        foreach(Area2D area in DropDetectors) area.QueueFree();
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
            int colBind = col-1;
            area.MouseExited += () => OnDropDetectorMouseExit(colBind);
            area.MouseEntered += () => OnDropDetectorMouseEnter(colBind);
            DropDetectorShapes.Add(shape);
            DropDetectors.Add(area);
            //add the areas directly after the board, so that the save/load buttons take priority
            GameBoard.AddSibling(area);
            area.GlobalPosition = center;
        }
    }

    public void SetDetectorsDisabled(bool disabled)
    {
        DropDetectorIdx = null;
        foreach(CollisionShape2D col in DropDetectorShapes)
            col.SetDeferredDisabled(disabled);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if(
            @event.IsJustPressed() && 
            @event is InputEventMouseButton mb &&
            DropDetectorIdx is not null &&
            _selectedControl is not null &&
            _selectedControl.CanTake() &&
            _selectedButton is not null
        )
        {
            if(mb.ButtonIndex == MouseButton.Left)
            {
                TokenBase t = Autoloads.ObjectPool.GetObject<TokenBase>(_selectedButton.AssociatedScene);
                t.TokenColor = TurnColor;
                if(GameBoard.AddToken((int)DropDetectorIdx, t))
                {
                    _selectedControl.Take(1);
                    PassTurn();
                }
            }
        }

        if(
            @event.IsJustPressed() &&
            @event is InputEventKey ek
        )
        {
            bool needGravity = true;
            bool needNewDetectors = true;
            switch(ek.Keycode)
            {
                case Key.W or Key.S:
                {
                    GameBoard.FlipVertical();
                    //show ghost token in correct position
                    GameBoard.QueueRedraw();
                    needNewDetectors = false;
                }
                break;
                case Key.A:
                {
                    GameBoard.RotateLeft();
                }
                break;
                case Key.D:
                {
                    GameBoard.RotateRight();
                }
                break;
                default:
                {
                    needGravity = false;
                }
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
        }
    }

    public void PassTurn()
    {
        Turn = NextTurn;
        _selectedControl = null;
        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
        Autoloads.EventBus.EmitSignal(EventBus.SignalName.TurnChanged, (int)Turn, false);
    }

    public void OnTokenSelected(TokenCounterControl what, TokenCounterButton who)
    {
        if(what.ActiveOnTurn != Turn || !what.CanTake()) return;
        _selectedControl = what;
        _selectedButton = who;

        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
    }

    public void DeserializeFrom(GameData data)
    {
        _selectedControl = null;
        DropDetectorIdx = null;
        Turn = data.Turn;
        if(data.Players.Count != CounterLists.Count)
            throw new ArgumentException($"Cannot deserialize game data with {data.Players.Count} players into game with {CounterLists.Count} players");
        if(data.Board is not null)
            GameBoard.DeserializeFrom(data.Board);
        for(int i = 0; i < CounterLists.Count; ++i)
            CounterLists[i].DeserializeFrom(data.Players[i]);
        //make sure stuff works correctly
        Autoloads.EventBus.EmitSignal(EventBus.SignalName.TurnChanged, (int)Turn, true);

        SetupDropDetectors();
        SetDetectorsDisabled(false);
    }

    public GameData SerializeTo() => new()
    {
        Turn = Turn,
        Board = GameBoard.SerializeTo(),
        Players = CounterLists.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
