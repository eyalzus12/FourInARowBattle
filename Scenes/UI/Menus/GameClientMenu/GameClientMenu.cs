using System;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class GameClientMenu : Node
{
    public const string CONNECTING_STATUS = "Connecting... Please Wait.";
    public const string CONNECTED_STATUS = "Connected!";
    public const string DISCONNECTED_STATUS = "Disconnected. Please try again in a few minutes.";

    #region Editor-Set Values

    [ExportCategory("Nodes")]
    [Export]
    private GameClient _client = null!;
    [Export(PropertyHint.File, "*.tscn,*.scn")]
    private string _mainMenu = "";
    [Export]
    private RemotePlayMenu _remotePlayMenu = null!;
    [Export]
    private LobbyMenu _lobbyMenu = null!;
    [Export]
    private GameMenu _gameMenu = null!;
    [Export]
    private Label _statusLabel = null!;
    [Export]
    private AcceptDialog _noticePopup = null!;
    [Export]
    private AcceptDialog _errorPopup = null!;
    [ExportCategory("")]
    [Export]
    private GameData _initialState = null!;

    #endregion

    private bool _inGame = false;
    private bool _inLobby = false;
    private bool _kickingToMainMenu = false;
    private bool _kickingToRemotePlayMenu = false;
    private bool _kickingToLobby = false;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_client);
        ArgumentNullException.ThrowIfNull(_remotePlayMenu);
        ArgumentNullException.ThrowIfNull(_lobbyMenu);
        ArgumentNullException.ThrowIfNull(_gameMenu);
        ArgumentNullException.ThrowIfNull(_statusLabel);
        ArgumentNullException.ThrowIfNull(_errorPopup);
        ArgumentNullException.ThrowIfNull(_noticePopup);
        ArgumentNullException.ThrowIfNull(_initialState);
    }

    private void ConnectSignals()
    {
        GetWindow().SizeChanged += OnWindowSizeChanged;
        _errorPopup.Confirmed += OnErrorPopupClosed;
        _errorPopup.Canceled += OnErrorPopupClosed;
        _noticePopup.Confirmed += OnNoticePopupClosed;
        _noticePopup.Canceled += OnNoticePopupClosed;
        _client.Connected += OnClientConnected;
        _client.Disconnected += OnClientDisconnected;
        _client.ServerClosed += OnClientServerClosed;
        _client.ErrorOccured += OnClientErrorOccured;
        _client.LobbyEntered += OnClientLobbyEntered;
        _client.LobbyPlayerJoined += OnClientLobbyPlayerJoined;
        _client.LobbyPlayerLeft += OnClientLobbyPlayerLeft;
        _client.LobbyTimeoutWarned += OnClientLobbyTimeoutWarned;
        _client.LobbyTimedOut += OnClientLobbyTimedOut;
        _client.GameEjected += OnClientGameEjected;
        _client.NewGameRequestSent += OnClientNewGameRequestSent;
        _client.NewGameRequestReceived += OnClientNewGameRequestReceived;
        _client.NewGameAcceptSent += OnClientNewGameAcceptSent;
        _client.NewGameAcceptReceived += OnClientNewGameAcceptReceived;
        _client.NewGameRejectSent += OnClientNewGameRejectSent;
        _client.NewGameRejectReceived += OnClientNewGameRejectReceived;
        _client.NewGameCancelSent += OnClientNewGameCancelSent;
        _client.NewGameCancelReceived += OnClientNewGameCancelReceived;
        _client.PlayerBecameBusy += OnClientPlayerBecameBusy;
        _client.PlayerBecameAvailable += OnClientPlayerBecameAvailable;
        _client.GameStarted += OnClientGameStarted;
        _client.GameActionPlaceSent += OnClientGameActionPlaceSent;
        _client.GameActionPlaceReceived += OnClientGameActionPlaceReceived;
        _client.GameActionRefillSent += OnClientGameActionRefillSent;
        _client.GameActionRefillReceived += OnClientGameActionRefillReceived;
        _client.GameFinished += OnClientGameFinished;
        _remotePlayMenu.CreateLobbyRequested += OnRemotePlayMenuCreateLobbyRequested;
        _remotePlayMenu.JoinLobbyRequested += OnRemotePlayMenuJoinLobbyRequested;
        _remotePlayMenu.LobbyNumberWasInvalid += OnRemotePlayMenuLobbyNumberWasInvalid;
        _remotePlayMenu.GoBackRequested += OnRemotePlayMenuGoBackRequested;
        _lobbyMenu.ExitLobbyRequested += OnLobbyMenuExitLobbyRequested;
        _lobbyMenu.ChallengeSent += OnLobbyMenuChallengeSent;
        _lobbyMenu.ChallengeCanceled += OnLobbyMenuChallengeCanceled;
        _lobbyMenu.ChallengeAccepted += OnLobbyMenuChallengeAccepted;
        _lobbyMenu.ChallengeRejected += OnLobbyMenuChallengeRejected;
        _gameMenu.TokenPlaceAttempted += OnGameMenuTokenPlaceAttempted;
        _gameMenu.RefillAttempted += OnGameMenuRefillAttempted;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        _statusLabel.Text = CONNECTING_STATUS;
    }


    #region Signal Handling

    private void OnWindowSizeChanged()
    {
        if(_errorPopup.Visible)
            _errorPopup.PopupCentered();
        if(_noticePopup.Visible)
            _noticePopup.PopupCentered();
    }

    private void OnErrorPopupClosed()
    {
        if(_kickingToMainMenu)
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, _mainMenu);
            _kickingToMainMenu = false;
        }

        if(_kickingToRemotePlayMenu)
        {
            SwitchToRemotePlayMenu();
            _kickingToRemotePlayMenu = false;
        }

        if(_kickingToLobby)
        {
            SwitchToLobbyMenu();
            _kickingToLobby = false;
        }
    }

    private void OnNoticePopupClosed()
    {
        if(_kickingToMainMenu)
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, _mainMenu);
            _kickingToMainMenu = false;
        }

        if(_kickingToRemotePlayMenu)
        {
            SwitchToRemotePlayMenu();
            _kickingToRemotePlayMenu = false;
        }

        if(_kickingToLobby)
        {
            SwitchToLobbyMenu();
            _kickingToLobby = false;
        }
    }

    private void OnClientConnected()
    {
        GD.Print("Connected!");
        SwitchToRemotePlayMenu();

        _statusLabel.Text = CONNECTED_STATUS;
    }

    private void OnClientDisconnected()
    {
        GD.Print("Connection closed");
        _kickingToMainMenu = true;
        DisplayError("Connection failed");

        _statusLabel.Text = DISCONNECTED_STATUS;
    }

    private void OnClientServerClosed()
    {
        GD.Print("Server closed");
        _kickingToMainMenu = true;
        DisplayNotice("Server Closed!");

        _statusLabel.Text = DISCONNECTED_STATUS;
    }

    private void OnClientErrorOccured(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        DisplayError(description);
    }

    private void OnClientLobbyEntered(uint lobbyId, Godot.Collections.Array<string> players, int index)
    {
        SwitchToLobbyMenu();
        _lobbyMenu.SetLobbyId(lobbyId);
        _lobbyMenu.SetPlayerNames(players.ToArray());
        _lobbyMenu.SetMark(index);
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, index);
    }

    private void OnClientLobbyPlayerJoined(string name)
    {
        _lobbyMenu.AddPlayer(name);
    }

    private void OnClientLobbyPlayerLeft(int index)
    {
        _lobbyMenu.RemovePlayer(index);
    }

    private void OnClientLobbyTimeoutWarned(int secondsRemaining)
    {
        DisplayNotice($"Lobby will timeout in {secondsRemaining} seconds");
    }

    private void OnClientLobbyTimedOut()
    {
        DisplayNotice("Lobby timed out");
        _kickingToRemotePlayMenu = true;
    }

    private void OnClientGameEjected()
    {
        DisplayNotice("Other player disconnected. Game ejected");
        _kickingToLobby = true;
    }

    private void OnClientNewGameRequestSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.SENT, playerIndex);
    }

    private void OnClientNewGameRequestReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.GOT, playerIndex);
    }

    private void OnClientNewGameAcceptSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    private void OnClientNewGameAcceptReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    private void OnClientNewGameRejectSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    private void OnClientNewGameRejectReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    private void OnClientNewGameCancelSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    private void OnClientNewGameCancelReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    private void OnClientPlayerBecameBusy(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    private void OnClientPlayerBecameAvailable(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    private void OnClientGameStarted(GameTurnEnum turn, int opponentIndex)
    {
        for(int i = 0; i < _lobbyMenu.PlayerCount; ++i)
            _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, i);
        SwitchToGame();
        _gameMenu.AllowedTurns = new(){turn};
        _gameMenu.InitGame();
    }

    private void OnClientGameActionPlaceSent(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _gameMenu.PlaceToken(column, scene);
    }

    private void OnClientGameActionPlaceReceived(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ErrorCodeEnum? err = _gameMenu.PlaceToken(column, scene);
        if(err is not null)
        {
            GD.Print($"Other player's placement produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            _client.Desync();
            return;
        }
    }

    private void OnClientGameActionRefillSent()
    {
        _gameMenu.Refill();
    }

    private void OnClientGameActionRefillReceived()
    {
        ErrorCodeEnum? err = _gameMenu.Refill();
        if(err is not null)
        {
            GD.Print($"Other player's refill produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            _client.Desync();
            return;
        }
    }

    private void OnClientGameFinished()
    {
        SwitchToLobbyMenu();
    }

    private void OnRemotePlayMenuCreateLobbyRequested(string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        if(playerName == "") playerName = "Guest";
        _client.ClientName = playerName;
        _client.CreateLobby();
    }

    private void OnRemotePlayMenuJoinLobbyRequested(uint id, string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        if(playerName == "") playerName = "Guest";
        _client.ClientName = playerName;
        _client.JoinLobby(id);
    }

    private void OnRemotePlayMenuLobbyNumberWasInvalid()
    {
        DisplayError("Invalid lobby number");
    }

    private void OnRemotePlayMenuGoBackRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnLobbyMenuExitLobbyRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(path != _remotePlayMenu.SceneFilePath)
        {
            GD.PushError($"Attempt to exit lobby into wrong scene {path}");
            return;
        }

        SwitchToRemotePlayMenu();
    }

    private void OnLobbyMenuChallengeSent(int index)
    {
        _client.RequestNewGame(index);
    }

    private void OnLobbyMenuChallengeCanceled(int index)
    {
        _client.CancelNewGame(index);
    }

    private void OnLobbyMenuChallengeAccepted(int index)
    {
        _client.AcceptNewGame(index);
    }

    private void OnLobbyMenuChallengeRejected(int index)
    {
        _client.RejectNewGame(index);
    }

    private void OnGameMenuTokenPlaceAttempted(int column, PackedScene token)
    {
        _client.PlaceToken((byte)column, token.ResourcePath);
    }

    private void OnGameMenuRefillAttempted()
    {
        _client.Refill();
    }

    #endregion
    
    #region Menu Operations

    private void SwitchToRemotePlayMenu()
    {
        _lobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        _lobbyMenu.Visible = false;

        _gameMenu.ProcessMode = ProcessModeEnum.Disabled;
        _gameMenu.Visible = false;

        _inGame = false;
        if(_inLobby)
            _client.DisconnectFromLobby(DisconnectReasonEnum.DESIRE);

        _remotePlayMenu.ProcessMode = ProcessModeEnum.Inherit;
        _remotePlayMenu.Visible = true;
    }

    private void SwitchToLobbyMenu()
    {
        _remotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        _remotePlayMenu.Visible = false;
        
        _gameMenu.ProcessMode = ProcessModeEnum.Disabled;
        _gameMenu.Visible = false;

        _lobbyMenu.ProcessMode = ProcessModeEnum.Inherit;
        _lobbyMenu.Visible = true;

        _inLobby = true;
    }

    private void SwitchToGame()
    {
        _remotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        _remotePlayMenu.Visible = false;

        _lobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        _lobbyMenu.Visible = false;
        
        _gameMenu.ProcessMode = ProcessModeEnum.Inherit;
        _gameMenu.Visible = true;

        _gameMenu.Game.DeserializeFrom(_initialState);

        _inGame = true;
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!_errorPopup.Visible)
        {
            _errorPopup.DialogText = error;
            _errorPopup.PopupCentered();
        }
    }

    private void DisplayNotice(string notice)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if(!_noticePopup.Visible)
        {
            _noticePopup.DialogText = notice;
            _noticePopup.PopupCentered();
        }
    }

    #endregion
}