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
    [Export]
    public Godot.Collections.Array<DescriptionLabel> DescriptionLabels{get; set;} = new();

    private readonly List<Area2D> _dropDetectors = new();
    private readonly List<CollisionShape2D> _dropDetectorShapes = new();

    private int? _dropDetectorIdx;
    public int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        private set
        {
            _dropDetectorIdx = value;
            if(Autoloads.PersistentData.HeadlessMode) return;
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

    private bool _droppingActive = false;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(GameBoard);
        foreach(TokenCounterListControl clist in CounterLists) ArgumentNullException.ThrowIfNull(clist);
        foreach(DescriptionLabel label in DescriptionLabels) ArgumentNullException.ThrowIfNull(label);
    }

    public override void _Ready()
    {
        VerifyExports();

        _droppingActive = true;
        if(!Autoloads.PersistentData.HeadlessMode)
        {
            SetupDropDetectors();
            SetDetectorsDisabled(false);
        }
        
        foreach(TokenCounterListControl clist in CounterLists)
        {
            clist.TokenSelected += OnTokenSelected;
            clist.RefilledTokens += PassTurn;
            clist.TokenButtonHovered += (GameTurnEnum turn, string description) =>
            {
                foreach(DescriptionLabel label in DescriptionLabels)
                {
                    label.OnTokenHover(turn, description);
                }
            };
            clist.TokenButtonStoppedHover += (GameTurnEnum turn, string description) =>
            {
                foreach(DescriptionLabel label in DescriptionLabels)
                {
                    label.OnTokenStopHover(turn, description);
                }
            };
        }

        //_Ready is called on children before the parent
        //so we can do this to signal the token counters
        //and update their disabled/enabled state
        foreach(TokenCounterListControl control in CounterLists)
            control.OnTurnChange(Turn);

        if(Autoloads.PersistentData.ContinueFromState is not null)
        {
            DeserializeFrom(Autoloads.PersistentData.ContinueFromState);
            Autoloads.PersistentData.ContinueFromState = null;
        }

        GameBoard.ScoreIncreased += (GameTurnEnum who, int amount) =>
        {            
            foreach(TokenCounterListControl counter in CounterLists)
                counter.OnAddScore(who, amount);
        };

        GameBoard.TokenStartedDrop += () =>
        {
            _droppingActive = false;
            GameBoard.HideGhostToken();
        };

        GameBoard.TokenFinishedDrop += () =>
        {
            _droppingActive = true;
            DropDetectorIdx = _dropDetectorIdx; //self assign to invoke ghost token display logic
        };
    }

    public void SetupDropDetectors()
    {
        if(Autoloads.PersistentData.HeadlessMode) return;
        DropDetectorIdx = null;
        foreach(Area2D area in _dropDetectors) area.QueueFree();
        _dropDetectors.Clear();
        _dropDetectorShapes.Clear();
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
            _dropDetectorShapes.Add(shape);
            _dropDetectors.Add(area);
            //add the areas directly after the board, so that the save/load buttons take priority
            GameBoard.AddSibling(area);
            area.GlobalPosition = center;
        }
    }

    public void SetDetectorsDisabled(bool disabled)
    {
        if(Autoloads.PersistentData.HeadlessMode) return;
        DropDetectorIdx = null;
        foreach(CollisionShape2D col in _dropDetectorShapes)
            col.SetDeferredDisabled(disabled);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if(Autoloads.PersistentData.HeadlessMode) return;

        if(
            @event.IsJustPressed() && 
            @event is InputEventMouseButton mb &&
            _droppingActive &&
            DropDetectorIdx is not null &&
            _selectedControl is not null &&
            _selectedControl.CanTake() &&
            _selectedButton is not null
        )
        {
            if(mb.ButtonIndex == MouseButton.Left)
            {
                TokenBase t = Autoloads.ScenePool.GetScene<TokenBase>(_selectedButton.AssociatedScene);
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
                //debug key
                case Key.F3:
                {
                    Autoloads.ScenePool.CleanPool();
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
        foreach(TokenCounterListControl counter in CounterLists) counter.OnTurnChange(Turn);
    }

    public void OnTokenSelected(TokenCounterControl what, TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(what);
        ArgumentNullException.ThrowIfNull(who);
        if(what.ActiveOnTurn != Turn || !what.CanTake()) return;
        _selectedControl = what;
        _selectedButton = who;

        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
    }

    public void DeserializeFrom(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.Board);

        _selectedControl = null;
        DropDetectorIdx = null;
        Turn = data.Turn;
        if(data.Players.Count != CounterLists.Count)
        {
            GD.PushError($"Cannot deserialize game data with {data.Players.Count} players into game with {CounterLists.Count} players");
            return;
        }
        
        GameBoard.DeserializeFrom(data.Board);
        for(int i = 0; i < CounterLists.Count; ++i)
        {
            ArgumentNullException.ThrowIfNull(data.Players[i]);
            CounterLists[i].DeserializeFrom(data.Players[i]);
        }
        //make sure stuff works correctly
        foreach(TokenCounterListControl counter in CounterLists) counter.OnTurnChange(Turn);

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
