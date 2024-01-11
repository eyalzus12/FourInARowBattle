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
    private SaveGameButton SaveGame = null!;
    [Export]
    private LoadGameButton LoadGame = null!;
    [ExportCategory("")]
    [Export]
    private bool InteractionEnabled = false;
    [Export]
    private bool LoadingEnabled = false;
    [Export]
    private bool SavingEnabled = false;
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
        ArgumentNullException.ThrowIfNull(LoadGame);
        ArgumentNullException.ThrowIfNull(SaveGame);
    }

    private void ConnectSignals()
    {
        Game.GameBoard.TokenFinishedDrop += OnGameGameBoardTokenFinishedDrop;
        Game.GameBoard.TokenStartedDrop += OnGameGameBoardTokenStartedDrop;
        Game.GhostTokenRenderWanted += OnGameGhostTokenRenderWanted;
        Game.GhostTokenHidingWanted += OnGameGhostTokenHidingWanted;
        Game.TokenPlaceAttempted += OnGameTokenPlaceAttempted;
        Game.RefillAttempted += OnGameRefillAttempted;
        LoadGame.GameLoadRequested += OnLoadGameGameLoadRequested;
        SaveGame.GameSaveRequested += OnSaveGameGameSaveRequested;
    }

    private void InitGame()
    {
        if(InteractionEnabled)
        {
            Game.SetupDropDetectors();
            Game.SetDetectorsDisabled(!_allowedTurns.Contains(Game.Turn));
        }
        Game.HideCountersOfTurns(_allowedTurns);
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        InitGame();
        SaveGame.Disabled = !SavingEnabled;
        SaveGame.Visible = SavingEnabled;
        LoadGame.Disabled = !LoadingEnabled;
        LoadGame.Visible = LoadingEnabled;
    }

    private void OnGameGameBoardTokenFinishedDrop()
    {
        SaveGame.Disabled = false;
    }

    private void OnGameGameBoardTokenStartedDrop()
    {
        SaveGame.Disabled = !SavingEnabled;
    }

    private void OnGameGhostTokenRenderWanted(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if(!InteractionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        Game.RenderGhostToken(texture, color, col);
    }

    private void OnGameGhostTokenHidingWanted()
    {
        if(!InteractionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        Game.HideGhostToken();
    }

    private void OnGameTokenPlaceAttempted(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if(!InteractionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        EmitSignal(SignalName.TokenPlaceAttempted, column, scene);
    }

    private void OnGameRefillAttempted()
    {
        if(!InteractionEnabled || !_allowedTurns.Contains(Game.Turn)) return;
        EmitSignal(SignalName.RefillAttempted);
    }

    private void OnLoadGameGameLoadRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!LoadingEnabled) return;
        GameData data = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        Game.DeserializeFrom(data);
        InitGame();
    }

    private void OnSaveGameGameSaveRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!SavingEnabled) return;
        GameData data = Game.SerializeTo();
        Error err = ResourceSaver.Save(data, path, ResourceSaver.SaverFlags.Compress);
        if(err != Error.Ok)
        {
            GD.PushError($"Got error {err} while trying to save game");
        }
    }

    public ErrorCodeEnum? PlaceToken(int column, PackedScene token)
    {
        ArgumentNullException.ThrowIfNull(token);
        ErrorCodeEnum? err = Game.PlaceToken(column, token);
        if(InteractionEnabled && err is not null)
        {
            Game.SetDetectorsDisabled(!_allowedTurns.Contains(Game.Turn));
        }
        return err;
    }

    public ErrorCodeEnum? Refill()
    {
        ErrorCodeEnum? err = Game.DoRefill();
        if(InteractionEnabled && err is not null)
        {
            Game.SetDetectorsDisabled(!_allowedTurns.Contains(Game.Turn));
        }
        return err;
    }

    public bool ValidColumn(int column)
    {
        return Game.ValidColumn(column);
    }
}