using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class GameMenu : Node2D
{
    [Signal]
    public delegate void TokenPlaceAttemptedEventHandler(int column, PackedScene token);
    [Signal]
    public delegate void RefillAttemptedEventHandler();

    [ExportCategory("Nodes")]
    [Export]
    public Game Game{get; private set;} = null!;
    [Export]
    private SaveGameButton _saveGameButton = null!;
    [Export]
    private LoadGameButton _loadGameButton = null!;
    [ExportCategory("")]
    [Export]
    private bool _interactionEnabled = false;
    [Export]
    private bool _loadingEnabled = false;
    [Export]
    private bool _savingEnabled = false;
    [Export]
    public Godot.Collections.Array<GameTurnEnum> AllowedTurns
    {
        get => _allowedTurns.ToGodotArray();
        set => _allowedTurns = value?.ToHashSet() ?? new();
    }

    public GameTurnEnum Turn => Game.Turn;

    private HashSet<GameTurnEnum> _allowedTurns = new();

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Game);
        ArgumentNullException.ThrowIfNull(_loadGameButton);
        ArgumentNullException.ThrowIfNull(_saveGameButton);
    }

    private void ConnectSignals()
    {
        Game.GameBoard.TokenFinishedDrop += OnGameGameBoardTokenFinishedDrop;
        Game.GameBoard.TokenStartedDrop += OnGameGameBoardTokenStartedDrop;
        Game.GhostTokenRenderWanted += OnGameGhostTokenRenderWanted;
        Game.GhostTokenHidingWanted += OnGameGhostTokenHidingWanted;
        Game.TokenPlaceAttempted += OnGameTokenPlaceAttempted;
        Game.RefillAttempted += OnGameRefillAttempted;
        Game.TurnChanged += OnGameTurnChanged;
        _loadGameButton.GameLoadRequested += OnLoadGameButtonGameLoadRequested;
        _saveGameButton.GameSaveRequested += OnSaveGameButtonGameSaveRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        InitGame();
        _saveGameButton.Disabled = !_savingEnabled;
        _saveGameButton.Visible = _savingEnabled;
        _loadGameButton.Disabled = !_loadingEnabled;
        _loadGameButton.Visible = _loadingEnabled;
    }

    private void OnGameGameBoardTokenFinishedDrop()
    {
        _saveGameButton.Disabled = false;
    }

    private void OnGameGameBoardTokenStartedDrop()
    {
        _saveGameButton.Disabled = !_savingEnabled;
    }

    private void OnGameGhostTokenRenderWanted(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if(!_interactionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        Game.RenderGhostToken(texture, color, col);
    }

    private void OnGameGhostTokenHidingWanted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        Game.HideGhostToken();
    }

    private void OnGameTokenPlaceAttempted(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if(!_interactionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        EmitSignal(SignalName.TokenPlaceAttempted, column, scene);
    }

    private void OnGameRefillAttempted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        EmitSignal(SignalName.RefillAttempted);
    }

    private void OnGameTurnChanged()
    {
        if(_interactionEnabled)
        {
            Game.SetDetectorsDisabled(!_allowedTurns.Contains(Game.Turn));
        }
    }

    private void OnLoadGameButtonGameLoadRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!_loadingEnabled) return;
        GameData data = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        Game.DeserializeFrom(data);
        InitGame();
    }

    private void OnSaveGameButtonGameSaveRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!_savingEnabled) return;
        GameData data = Game.SerializeTo();
        Error err = ResourceSaver.Save(data, path, ResourceSaver.SaverFlags.Compress);
        if(err != Error.Ok)
        {
            GD.PushError($"Got error {err} while trying to save game");
        }
    }

    public void InitGame()
    {
        if(_interactionEnabled)
        {
            Game.SetupDropDetectors();
            Game.SetDetectorsDisabled(!_allowedTurns.Contains(Game.Turn));
        }
        Game.ForceDisableCountersWithoutApprovedTurns(_allowedTurns);
    }

    public ErrorCodeEnum? PlaceToken(int column, PackedScene token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Game.PlaceToken(column, token);
    }

    public ErrorCodeEnum? Refill()
    {
        return Game.DoRefill();
    }

    public bool ValidColumn(int column)
    {
        return Game.ValidColumn(column);
    }
}