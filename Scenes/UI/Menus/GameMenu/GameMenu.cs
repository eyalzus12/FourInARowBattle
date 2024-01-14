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
    [Signal]
    public delegate void GameQuitRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private Game _game = null!;
    [Export]
    private GoBackButton _quitGameButton = null!;
    [Export]
    private ConfirmationDialog _confirmQuitDialog = null!;
    [Export]
    private Label _player1Label = null!;
    [Export]
    private Label _player2Label = null!;
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

    public GameTurnEnum Turn => _game.Turn;

    private HashSet<GameTurnEnum> _allowedTurns = new();

    private string? _goBackRequestPath;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_game);
        ArgumentNullException.ThrowIfNull(_player1Label);
        ArgumentNullException.ThrowIfNull(_player2Label);
        ArgumentNullException.ThrowIfNull(_loadGameButton);
        ArgumentNullException.ThrowIfNull(_saveGameButton);
        ArgumentNullException.ThrowIfNull(_quitGameButton);
        ArgumentNullException.ThrowIfNull(_confirmQuitDialog);
    }

    private void ConnectSignals()
    {
        GetWindow().SizeChanged += OnWindowSizeChanged;
        _game.GhostTokenRenderWanted += OnGameGhostTokenRenderWanted;
        _game.GhostTokenHidingWanted += OnGameGhostTokenHidingWanted;
        _game.TokenPlaceAttempted += OnGameTokenPlaceAttempted;
        _game.RefillAttempted += OnGameRefillAttempted;
        _game.TurnChanged += OnGameTurnChanged;
        _game.TokenFinishedDrop += OnGameTokenFinishedDrop;
        _game.TokenStartedDrop += OnGameTokenStartedDrop;
        _quitGameButton.ChangeSceneRequested += OnQuitButtonPressed;
        _confirmQuitDialog.Confirmed += OnQuitConfirmed;
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

    private void OnWindowSizeChanged()
    {
        if(_confirmQuitDialog.Visible)
            OnQuitButtonPressed(_goBackRequestPath!);
    }

    private void OnGameTokenFinishedDrop()
    {
        _saveGameButton.Disabled = false;
    }

    private void OnGameTokenStartedDrop()
    {
        _saveGameButton.Disabled = !_savingEnabled;
    }

    private void OnGameGhostTokenRenderWanted(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        _game.RenderGhostToken(texture, color, col);
    }

    private void OnGameGhostTokenHidingWanted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        _game.HideGhostToken();
    }

    private void OnGameTokenPlaceAttempted(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        EmitSignal(SignalName.TokenPlaceAttempted, column, scene);
    }

    private void OnGameRefillAttempted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        EmitSignal(SignalName.RefillAttempted);
    }

    private void OnGameTurnChanged()
    {
        if(_interactionEnabled)
        {
            _game.SetDetectorsDisabled(!_allowedTurns.Contains(_game.Turn));
        }
    }

    private void OnQuitButtonPressed(string goBackRequestPath)
    {
        ArgumentNullException.ThrowIfNull(goBackRequestPath);
        _goBackRequestPath = goBackRequestPath;
        _confirmQuitDialog.PopupCentered();
    }

    private void OnQuitConfirmed()
    {
        if(_goBackRequestPath is null) return;
        EmitSignal(SignalName.GameQuitRequested, _goBackRequestPath);
    }

    private void OnLoadGameButtonGameLoadRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!_loadingEnabled) return;
        GameData data = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        _game.DeserializeFrom(data);
        InitGame();
    }

    private void OnSaveGameButtonGameSaveRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!_savingEnabled) return;
        GameData data = _game.SerializeTo();
        Error err = ResourceSaver.Save(data, path, ResourceSaver.SaverFlags.Compress);
        if(err != Error.Ok)
        {
            GD.PushError($"Got error {err} while trying to save game");
        }
    }

    public void SetPlayers(string player1, string player2, bool whoAmI)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(player2);
        _player1Label.Text = player1;
        _player1Label.Modulate = whoAmI ? Colors.Cyan : Colors.White;
        _player2Label.Text = player2;
        _player2Label.Modulate = whoAmI ? Colors.White : Colors.Cyan;
    }

    public void InitGame()
    {
        if(_interactionEnabled)
        {
            _game.SetupDropDetectors();
            _game.SetDetectorsDisabled(!_allowedTurns.Contains(_game.Turn));
        }
        _game.ForceDisableCountersWithoutApprovedTurns(_allowedTurns);
    }

    public ErrorCodeEnum? PlaceToken(int column, PackedScene token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _game.PlaceToken(column, token);
    }

    public ErrorCodeEnum? Refill()
    {
        return _game.DoRefill();
    }

    public bool ValidColumn(int column)
    {
        return _game.ValidColumn(column);
    }

    public void DeserializeFrom(GameData data)
    {
        _game.DeserializeFrom(data);
    }
}