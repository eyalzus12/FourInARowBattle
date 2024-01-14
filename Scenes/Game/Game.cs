using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

/// <summary>
/// This class is the base Game class. The class has signals for the basic actions.
/// It does not perform those actions itself to allow further checking and control through GameMenu.
/// 
/// The game UI has the following hierarchy:
/// TokenCounterListControl - This class contains a list of tokens counters, and the refill button. Each player has 1.
/// TokenCounterControl - The individual controls which show the number of tokens and allow selecting the,.
/// TokenCounterButton - The buttons of the TokenCounterControl.
/// </summary>
public partial class Game : Node2D
{
    /// <summary>
    /// Render ghost token desired
    /// </summary>
    /// <param name="texture">Token texture</param>
    /// <param name="color">Token color</param>
    /// <param name="col">The column</param>
    [Signal]
    public delegate void GhostTokenRenderWantedEventHandler(Texture2D texture, Color color, int col);
    /// <summary>
    /// Hide ghost token desired
    /// </summary>
    [Signal]
    public delegate void GhostTokenHidingWantedEventHandler();
    /// <summary>
    /// Placing token desired
    /// </summary>
    /// <param name="column">Token column</param>
    /// <param name="token">Token scene</param>
    [Signal]
    public delegate void TokenPlaceAttemptedEventHandler(int column, PackedScene token);
    /// <summary>
    /// Refill desired
    /// </summary>
    [Signal]
    public delegate void RefillAttemptedEventHandler();
    /// <summary>
    /// Turn changed
    /// </summary>
    [Signal]
    public delegate void TurnChangedEventHandler();
    /// <summary>
    /// Token started dropping
    /// </summary>
    [Signal]
    public delegate void TokenStartedDropEventHandler();
    /// <summary>
    /// Token finished dropping
    /// </summary>
    [Signal]
    public delegate void TokenFinishedDropEventHandler();
    
    private TokenCounterControl? _selectedControl = null;
    private TokenCounterButton? _selectedButton = null;

    private GameTurnEnum _turn = GameTurnEnum.PLAYER1;
    /// <summary>
    /// The current game turn
    /// </summary>
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
        GameTurnEnum.PLAYER1 => GameTurnEnum.PLAYER2,
        GameTurnEnum.PLAYER2 => GameTurnEnum.PLAYER1,
        _ => throw new ArgumentException($"Invalid turn {Turn}")
    };

    [ExportCategory("Nodes")]
    [Export]
    private Board _gameBoard = null!;
    [Export]
    private Godot.Collections.Array<TokenCounterListControl> _counterLists = new();
    [Export]
    private Godot.Collections.Array<DescriptionLabel> _descriptionLables = new();
    /// <summary>
    /// The vertical offset for the press detectors
    /// </summary>
    [ExportCategory("")]
    [Export]
    private float _pressDetectorOffset = 200;

    private readonly List<Area2D> _dropDetectors = new();
    private readonly List<CollisionShape2D> _dropDetectorShapes = new();

    private int? _dropDetectorIdx;
    /// <summary>
    /// The current hovered column
    /// </summary>
    private int? DropDetectorIdx
    {
        get => _dropDetectorIdx;
        set
        {
            _dropDetectorIdx = value;
            if(value is not null &&
                _selectedControl is not null &&
                _selectedControl.CanTake() &&
                _selectedButton is not null)
            {
                //a little hack: use the button's icon so we don't have to open up the scene
                //and fetch the texture
                EmitSignal(SignalName.GhostTokenRenderWanted, _selectedButton.Icon, TurnColor, (int)value);
            }
            else
            {
                EmitSignal(SignalName.GhostTokenHidingWanted);
            }
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
        ArgumentNullException.ThrowIfNull(_gameBoard);
        foreach(TokenCounterListControl clist in _counterLists) ArgumentNullException.ThrowIfNull(clist);
        foreach(DescriptionLabel label in _descriptionLables) ArgumentNullException.ThrowIfNull(label);
    }

    private void ConnectSignals()
    {
        foreach(TokenCounterListControl clist in _counterLists)
        {
            clist.TokenSelected += OnTokenSelected;
            clist.RefillAttempted += OnRefillAttempted;
            clist.TokenButtonHovered += OnTokenButtonHovered;
            clist.TokenButtonStoppedHover += OnTokenButtonStoppedHover;
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

        _gameBoard.ScoreIncreased += OnScoreIncreased;
        _gameBoard.TokenStartedDrop += OnTokenStartedDrop;
        _gameBoard.TokenFinishedDrop += OnTokenFinishedDrop;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();

        _droppingActive = true;
    }

    #region Signal Handling
    
    /// <summary>
    /// Token has been selected
    /// </summary>
    /// <param name="what">What token counter</param>
    /// <param name="who">What token button</param>
    public void OnTokenSelected(TokenCounterControl what, TokenCounterButton who)
    {
        ArgumentNullException.ThrowIfNull(what);
        ArgumentNullException.ThrowIfNull(who);
        //button has wrong turn or not enough tokens
        if(what.ActiveOnTurn != Turn || !what.CanTake()) return;
        _selectedControl = what;
        _selectedButton = who;

        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
    }

    /// <summary>
    /// Refill button pressed
    /// </summary>
    private void OnRefillAttempted()
    {
        EmitSignal(SignalName.RefillAttempted);
    }

    /// <summary>
    /// Token button hovered. Show token description.
    /// </summary>
    /// <param name="turn">The turn of the hovered button</param>
    /// <param name="description">The description of the token</param>
    private void OnTokenButtonHovered(GameTurnEnum turn, string description)
    {
        foreach(DescriptionLabel label in _descriptionLables)
        {
            label.OnTokenHover(turn, description);
        }
    }

    /// <summary>
    /// Token button no longer hovered. Clear token description.
    /// </summary>
    /// <param name="turn">The turn of the no-longer-hovered button</param>
    /// <param name="description">The description of the token</param>
    private void OnTokenButtonStoppedHover(GameTurnEnum turn, string description)
    {
        foreach(DescriptionLabel label in _descriptionLables)
        {
            label.OnTokenStopHover(turn, description);
        }
    }

    /// <summary>
    /// Score increased
    /// </summary>
    /// <param name="who">For who</param>
    /// <param name="amount">How much</param>
    private void OnScoreIncreased(GameTurnEnum who, int amount)
    {            
        foreach(TokenCounterListControl counter in _counterLists)
            counter.OnAddScore(who, amount);
    }

    /// <summary>
    /// Token started dropping
    /// </summary>
    private void OnTokenStartedDrop()
    {
        _droppingActive = false;
        _gameBoard.HideGhostToken();
        EmitSignal(SignalName.TokenStartedDrop);
    }

    /// <summary>
    /// Token stopped dropping
    /// </summary>
    private void OnTokenFinishedDrop()
    {
        _droppingActive = true;
        DropDetectorIdx = _dropDetectorIdx; //self assign to invoke ghost token display logic
        EmitSignal(SignalName.TokenFinishedDrop);
    }

    #endregion

    /// <summary>
    /// Setup the areas that detect presses
    /// </summary>
    public void SetupDropDetectors()
    {
        DropDetectorIdx = null;
        foreach(Area2D area in _dropDetectors) area.QueueFree();
        _dropDetectors.Clear();
        _dropDetectorShapes.Clear();
        for(int col = 1; col <= _gameBoard.Columns; ++col)
        {
            Vector2 topMost = _gameBoard.HolePosition(0,col);
            Vector2 botMost = _gameBoard.HolePosition(_gameBoard.Rows+1,col);
            Vector2 center = (topMost + botMost)/2;
            Area2D area = new(){Monitorable = false};
            CollisionShape2D shape = new()
            {
                Shape = new RectangleShape2D(){Size = new(2*_gameBoard.SlotRadius, (botMost-topMost).Y + _pressDetectorOffset)},
                Disabled = true
            };
            area.AddChild(shape);
            int colBind = col-1;
            area.MouseExited += () => OnDropDetectorMouseExit(colBind);
            area.MouseEntered += () => OnDropDetectorMouseEnter(colBind);
            _dropDetectorShapes.Add(shape);
            _dropDetectors.Add(area);
            //add the areas directly after the board, so that the save/load buttons take priority
            _gameBoard.AddSibling(area);
            area.GlobalPosition = center;
        }
    }

    /// <summary>
    /// Disable or enable the areas that detect presses
    /// </summary>
    /// <param name="disabled">Whether to disable</param>
    public void SetDetectorsDisabled(bool disabled)
    {
        foreach(CollisionShape2D col in _dropDetectorShapes)
            col.SetDeferredDisabled(disabled);
    }

    /// <summary>
    /// Force disable (disable even if not disabled) the counters that aren't from a certain turn set.
    /// Used to disable the opponent's buttons when playing remotely.
    /// </summary>
    /// <param name="turns">The set of turns</param>
    public void ForceDisableCountersWithoutApprovedTurns(IReadOnlySet<GameTurnEnum> turns)
    {
        foreach(TokenCounterListControl clist in _counterLists)
        {
            clist.SetCountersForceDisabled(!turns.Contains(clist.ActiveOnTurn));
        }
    }

    /// <summary>
    /// Render a ghost token
    /// </summary>
    /// <param name="texture">The token texture</param>
    /// <param name="color">The token color</param>
    /// <param name="col">The column</param>
    public void RenderGhostToken(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _gameBoard.RenderGhostToken(texture, color, col);
    }

    /// <summary>
    /// Hide the ghost token
    /// </summary>
    public void HideGhostToken()
    {
        _gameBoard.HideGhostToken();
    }

    /// <summary>
    /// Place a token
    /// </summary>
    /// <param name="column">The column to place in</param>
    /// <param name="scene">The scene to place</param>
    /// <returns>An error code, or null if there's no error</returns>
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
        if(!_gameBoard.AddToken(column, token))
        {
            return ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN;
        }
        
        control.Take(1);
        PassTurn();
        return null;
    }

    /// <summary>
    /// Refill
    /// </summary>
    /// <returns>An error code or null if there are no errors</returns>
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
        
        //press input
        if( //left click
            @event.IsJustPressed() && 
            @event is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Left &&
            //can drop that token
            _droppingActive &&
            DropDetectorIdx is not null &&
            _selectedControl is not null &&
            _selectedControl.CanTake() &&
            _selectedButton is not null)
        {
            EmitSignal(SignalName.TokenPlaceAttempted, (int)DropDetectorIdx, _selectedButton.AssociatedScene);
        }
    }

    /// <summary>
    /// Pass the current turn
    /// </summary>
    public void PassTurn()
    {
        Turn = NextTurn;
        _selectedControl = null;
        _selectedButton = null;
        foreach(TokenCounterListControl counter in _counterLists) counter.OnTurnChange(Turn);
        //force redraw of ghost token
        DropDetectorIdx = _dropDetectorIdx;
    }

    /// <summary>
    /// Check if a column number is valid
    /// </summary>
    /// <param name="column">The column</param>
    /// <returns>Whether it is valid</returns>
    public bool ValidColumn(int column)
    {
        return 0 <= column && column < _gameBoard.Columns;
    }

    /// <summary>
    /// Load game data
    /// </summary>
    /// <param name="data">The game data</param>
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
        
        _gameBoard.DeserializeFrom(data.Board);
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

    /// <summary>
    /// Save current game state
    /// </summary>
    /// <returns>The game state</returns>
    public GameData SerializeTo() => new()
    {
        Turn = Turn,
        Board = _gameBoard.SerializeTo(),
        Players = _counterLists.Select(c => c.SerializeTo()).ToGodotArray()
    };
}
