using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

public partial class Game : Node2D
{
    [Signal]
    public delegate void GhostTokenRenderWantedEventHandler(Texture2D texture, Color color, int col);
    [Signal]
    public delegate void GhostTokenHidingWantedEventHandler();
    [Signal]
    public delegate void TokenPlaceAttemptedEventHandler(int column, PackedScene token);
    [Signal]
    public delegate void RefillAttemptedEventHandler();
    [Signal]
    public delegate void TurnChangedEventHandler();
    
    private TokenCounterControl? _selectedControl = null;
    private TokenCounterButton? _selectedButton = null;

    private GameTurnEnum _turn = GameTurnEnum.Player1;
    public GameTurnEnum Turn
    {
        get => _turn; 
        set
        {
            _turn = value;
            EmitSignal(SignalName.TurnChanged);
        }
    }
    private Color TurnColor => Turn.GameTurnToColor();

    private GameTurnEnum NextTurn => Turn switch
    {
        GameTurnEnum.Player1 => GameTurnEnum.Player2,
        GameTurnEnum.Player2 => GameTurnEnum.Player1,
        _ => throw new ArgumentException($"Invalid turn {Turn}")
    };

    [ExportCategory("Nodes")]
    [Export]
    public Board GameBoard{get; set;} = null!;
    [Export]
    private Godot.Collections.Array<TokenCounterListControl> _counterLists = new();
    [Export]
    private Godot.Collections.Array<DescriptionLabel> _descriptionLables = new();
    [ExportCategory("")]
    [Export]
    private float _pressDetectorOffset = 200;

    private readonly List<Area2D> _dropDetectors = new();
    private readonly List<CollisionShape2D> _dropDetectorShapes = new();

    private int? _dropDetectorIdx;
    private int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        set
        {
            _dropDetectorIdx = value;
            if(
                value is not null &&
                _selectedControl is not null &&
                _selectedControl.CanTake() &&
                _selectedButton is not null
            )
                //a little hack: use the button's icon so we don't have to open up the scene
                //and fetch the texture
                EmitSignal(SignalName.GhostTokenRenderWanted, _selectedButton.Icon, TurnColor, (int)value);
            else
                EmitSignal(SignalName.GhostTokenHidingWanted);
        }
    }

    private void OnDropDetectorMouseEnter(int col)
    {
        DropDetectorIdx = col;
    }

    private void OnDropDetectorMouseExit(int col)
    {
        if(DropDetectorIdx == col)
            DropDetectorIdx = null;
    }

    private bool _droppingActive = false;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(GameBoard);
        foreach(TokenCounterListControl clist in _counterLists) ArgumentNullException.ThrowIfNull(clist);
        foreach(DescriptionLabel label in _descriptionLables) ArgumentNullException.ThrowIfNull(label);
    }

    private void ConnectSignals()
    {
        foreach(TokenCounterListControl clist in _counterLists)
        {
            clist.TokenSelected += OnTokenSelected;
            clist.RefillAttempted += () =>
            {
                EmitSignal(SignalName.RefillAttempted);
            };
            clist.TokenButtonHovered += (GameTurnEnum turn, string description) =>
            {
                foreach(DescriptionLabel label in _descriptionLables)
                {
                    label.OnTokenHover(turn, description);
                }
            };
            clist.TokenButtonStoppedHover += (GameTurnEnum turn, string description) =>
            {
                foreach(DescriptionLabel label in _descriptionLables)
                {
                    label.OnTokenStopHover(turn, description);
                }
            };
        }

        //_Ready is called on children before the parent
        //so we can do this to signal the token counters
        //and update their disabled/enabled state
        foreach(TokenCounterListControl control in _counterLists)
            control.OnTurnChange(Turn);

        if(Autoloads.PersistentData.ContinueFromState is not null)
        {
            DeserializeFrom(Autoloads.PersistentData.ContinueFromState);
            Autoloads.PersistentData.ContinueFromState = null;
        }

        GameBoard.ScoreIncreased += (GameTurnEnum who, int amount) =>
        {            
            foreach(TokenCounterListControl counter in _counterLists)
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

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();

        _droppingActive = true;
    }

    public void SetupDropDetectors()
    {
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
                Shape = new RectangleShape2D(){Size = new(2*GameBoard.SlotRadius, (botMost-topMost).Y + _pressDetectorOffset)},
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
        DropDetectorIdx = null;
        foreach(CollisionShape2D col in _dropDetectorShapes)
            col.SetDeferredDisabled(disabled);
    }

    public void ForceDisableCountersWithoutApprovedTurns(IReadOnlySet<GameTurnEnum> turns)
    {
        foreach(TokenCounterListControl clist in _counterLists)
        {
            clist.SetCountersForceDisabled(!turns.Contains(clist.ActiveOnTurn));
        }
    }

    public void RenderGhostToken(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        GameBoard.RenderGhostToken(texture, color, col);
    }

    public void HideGhostToken()
    {
        GameBoard.HideGhostToken();
    }

    public ErrorCodeEnum? PlaceToken(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        TokenBase? token = Autoloads.ScenePool.GetSceneOrNull<TokenBase>(scene);
        //scene is not a token
        if(token is null)
        {
            return ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN;
        }
        //find control
        TokenCounterControl? control = null;
        foreach(TokenCounterListControl lc in _counterLists)
        {
            if(lc.ActiveOnTurn != Turn)
                continue;
            control ??= lc.FindCounterOfScene(scene);
            if(control is not null) break;
        }
        //attempt to use unusable token
        if(control is null)
        {
            return ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN;
        }
        //not enough tokens to use
        if(!control.CanTake())
        {
            return ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS;
        }

        token.TokenColor = TurnColor;
        if(!GameBoard.AddToken(column, token))
        {
            return ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN;
        }
        
        control.Take(1);
        PassTurn();
        return null;
    }

    public ErrorCodeEnum? DoRefill()
    {
        bool refillFailedBecauseFull = true;
        bool refillFailedBecauseLocked = true;
        foreach(TokenCounterListControl lc in _counterLists)
        {
            if(lc.ActiveOnTurn != Turn)
                continue;
            if(lc.AnyCanAdd())
            {
                refillFailedBecauseFull = false;
                bool success = lc.DoRefill();
                if(success)
                {
                    refillFailedBecauseLocked = false;
                }
            }
        }
        if(refillFailedBecauseFull)
            return ErrorCodeEnum.CANNOT_REFILL_ALL_FILLED;
        if(refillFailedBecauseLocked)
            return ErrorCodeEnum.CANNOT_REFILL_TWO_TURN_STREAK;
        PassTurn();
        return null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        //if(Autoloads.PersistentData.HeadlessMode) return;

        if(
            @event.IsJustPressed() && 
            @event is InputEventMouseButton mb &&
            //!IsTurnDisabled(Turn) &&
            _droppingActive &&
            DropDetectorIdx is not null &&
            _selectedControl is not null &&
            _selectedControl.CanTake() &&
            _selectedButton is not null
        )
        {
            if(mb.ButtonIndex == MouseButton.Left)
            {
                EmitSignal(SignalName.TokenPlaceAttempted, (int)DropDetectorIdx, _selectedButton.AssociatedScene);
            }
        }

        /*if(
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
        }*/
    }

    public void PassTurn()
    {
        Turn = NextTurn;
        _selectedControl = null;
        _selectedButton = null;
        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
        foreach(TokenCounterListControl counter in _counterLists) counter.OnTurnChange(Turn);
    }

    public bool ValidColumn(int column)
    {
        return 0 <= column && column < GameBoard.Columns;
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
        _selectedButton = null;
        DropDetectorIdx = null;
        Turn = data.Turn;
        if(data.Players.Count != _counterLists.Count)
        {
            GD.PushError($"Cannot deserialize game data with {data.Players.Count} players into game with {_counterLists.Count} players");
            return;
        }
        
        GameBoard.DeserializeFrom(data.Board);
        for(int i = 0; i < _counterLists.Count; ++i)
        {
            ArgumentNullException.ThrowIfNull(data.Players[i]);
            _counterLists[i].DeserializeFrom(data.Players[i]);
        }
        //make sure stuff works correctly
        foreach(TokenCounterListControl counter in _counterLists)
        {
            counter.OnTurnChange(Turn);
        }
    }

    public GameData SerializeTo() => new()
    {
        Turn = Turn,
        Board = GameBoard.SerializeTo(),
        Players = _counterLists.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
