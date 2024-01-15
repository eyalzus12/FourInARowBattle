using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// This is the UI between the Game and the server interaction.
/// It does not do the operations itself, only sends signals out and waits for function calls from outside.
/// For local play, GameMenuLocal is used, which connects those signals into its own functions.
/// 
/// The purpose of this class is to separate the logic of locking turns/interaction.
/// Interaction lock is used on the server while turn lock is used on the client.
/// </summary>
public partial class GameMenu : Node2D
{
    /// <summary>
    /// Token placing was attempted
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="token">The token scene</param>
    [Signal]
    public delegate void TokenPlaceAttemptedEventHandler(int column, PackedScene token);
    /// <summary>
    /// Refill was attempted
    /// </summary>
    [Signal]
    public delegate void RefillAttemptedEventHandler();
    /// <summary>
    /// Game quit was requested
    /// </summary>
    /// <param name="path">The path to the lobby scene</param>
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
    /// <summary>
    /// Whether it is possible to interact with the game. Used on the server.
    /// </summary>
    [ExportCategory("")]
    [Export]
    private bool _interactionEnabled = false;
    /// <summary>
    /// Whether it is possible to load a game. Only available in local.
    /// </summary>
    [Export]
    private bool _loadingEnabled = false;
    /// <summary>
    /// Whether it is possible to save the game. Available in all places but the server.
    /// </summary>
    [Export]
    private bool _savingEnabled = false;
    /// <summary>
    /// What turns are allowed to be interacted with
    /// </summary>
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

    /// <summary>
    /// Event: Window size changed. Resize popups.
    /// </summary>
    private void OnWindowSizeChanged()
    {
        if(_confirmQuitDialog.Visible)
            OnQuitButtonPressed(_goBackRequestPath!);
    }

    /// <summary>
    /// Event: Token finished dropping. Re-enable save button.
    /// </summary>
    private void OnGameTokenFinishedDrop()
    {
        _saveGameButton.Disabled = !_savingEnabled;
    }

    /// <summary>
    /// Event: Token started dropping. Disable save button.
    /// </summary>
    private void OnGameTokenStartedDrop()
    {
        _saveGameButton.Disabled = true;
    }

    /// <summary>
    /// Event: Game wants to render a ghost token. Check if turn is allowed.
    /// </summary>
    /// <param name="texture">The ghost token texture</param>
    /// <param name="color">The ghost token color</param>
    /// <param name="col">The column</param>
    private void OnGameGhostTokenRenderWanted(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        _game.RenderGhostToken(texture, color, col);
    }

    /// <summary>
    /// Event: Game wants to hide ghost token.
    /// </summary>
    private void OnGameGhostTokenHidingWanted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        _game.HideGhostToken();
    }

    /// <summary>
    /// Event: Game wants to place a token. Check if turn is allowed and emit signals.
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="scene">The token scene</param>
    private void OnGameTokenPlaceAttempted(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        EmitSignal(SignalName.TokenPlaceAttempted, column, scene);
    }

    /// <summary>
    /// Event: Game wants to refill. Check if turn is allowed and emit signals.
    /// </summary>
    private void OnGameRefillAttempted()
    {
        if(!_interactionEnabled || !_allowedTurns.Contains(_game.Turn)) return;
        EmitSignal(SignalName.RefillAttempted);
    }

    /// <summary>
    /// Event: Turn changed. Disable pressing if new turn is not allowed
    /// </summary>
    private void OnGameTurnChanged()
    {
        if(_interactionEnabled)
        {
            _game.SetDetectorsDisabled(!_allowedTurns.Contains(_game.Turn));
        }
    }

    /// <summary>
    /// Event: Quit button pressed. Show confirmation dialog.
    /// </summary>
    /// <param name="goBackRequestPath">The path to the lobby scene</param>
    private void OnQuitButtonPressed(string goBackRequestPath)
    {
        ArgumentNullException.ThrowIfNull(goBackRequestPath);
        _goBackRequestPath = goBackRequestPath;
        _confirmQuitDialog.PopupCentered();
    }

    /// <summary>
    /// Event: Quitting was confirmed. Emit signal.
    /// </summary>
    private void OnQuitConfirmed()
    {
        if(_goBackRequestPath is null) return;
        EmitSignal(SignalName.GameQuitRequested, _goBackRequestPath);
    }

    /// <summary>
    /// Event: User wants to load game state
    /// </summary>
    /// <param name="path">The path to the game data</param>
    private void OnLoadGameButtonGameLoadRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!_loadingEnabled) return;
        GameData data = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        _game.DeserializeFrom(data);
        InitGame();
    }

    /// <summary>
    /// Event: User wants to save game state
    /// </summary>
    /// <param name="path">The path to save to</param>
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

    /// <summary>
    /// Set the names of the players and mark whoever is us. Used for remote play.
    /// </summary>
    /// <param name="player1">Player 1 name</param>
    /// <param name="player2">Player 2 name</param>
    /// <param name="whoAmI">Which player is us</param>
    public void SetPlayers(string player1, string player2, bool whoAmI)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(player2);
        _player1Label.Text = player1;
        _player1Label.Modulate = whoAmI ? Colors.Cyan : Colors.White;
        _player2Label.Text = player2;
        _player2Label.Modulate = whoAmI ? Colors.White : Colors.Cyan;
    }

    /// <summary>
    /// Initialize game
    /// </summary>
    public void InitGame()
    {
        if(_interactionEnabled)
        {
            _game.SetupDropDetectors();
            _game.SetDetectorsDisabled(!_allowedTurns.Contains(_game.Turn));
        }
        _game.ForceDisableCountersWithoutApprovedTurns(_allowedTurns);
    }

    /// <summary>
    /// Try placing a token
    /// </summary>
    /// <param name="column">The column to place in</param>
    /// <param name="token">The token scene</param>
    /// <returns>An error or null if there's none</returns>
    public ErrorCodeEnum? PlaceToken(int column, PackedScene token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _game.PlaceToken(column, token);
    }

    /// <summary>
    /// Try refilling
    /// </summary>
    /// <returns>An error or null if there's none</returns>
    public ErrorCodeEnum? Refill()
    {
        return _game.DoRefill();
    }

    /// <summary>
    /// Check if a column is valid
    /// </summary>
    /// <param name="column">The column to check</param>
    /// <returns>Whether the column is valid</returns>
    public bool ValidColumn(int column)
    {
        return _game.ValidColumn(column);
    }

    /// <summary>
    /// Load game state
    /// </summary>
    /// <param name="data">The game state to load</param>
    public void DeserializeFrom(GameData data)
    {
        _game.DeserializeFrom(data);
    }
}