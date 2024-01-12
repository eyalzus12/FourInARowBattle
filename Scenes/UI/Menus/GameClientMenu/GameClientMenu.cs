using System;
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
        _client.LobbyStateUpdated += OnClientLobbyStateUpdated;
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

    private void OnClientLobbyEntered(uint lobbyId, string? player1Name, string? player2Name, bool isPlayer1)
    {
        SwitchToLobbyMenu();
        _lobbyMenu.SetLobbyId(lobbyId);
        _lobbyMenu.SetPlayer1Name(player1Name ?? "Guest");
        _lobbyMenu.SetPlayer2Name(player2Name ?? "Guest");

        if(isPlayer1) _lobbyMenu.SetPlayer1Marked();
        else _lobbyMenu.SetPlayer2Marked();

        if(_lobbyMenu.LobbyFull())
            _lobbyMenu.SetChallengeState_NoChallenge();
        else
            _lobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientLobbyStateUpdated(string? player1Name, string? player2Name, bool isPlayer1)
    {
        bool lobbyPreviouslyEmpty = !_lobbyMenu.LobbyFull();
        _lobbyMenu.SetPlayer1Name(player1Name ?? "Guest");
        _lobbyMenu.SetPlayer2Name(player2Name ?? "Guest");

        if(isPlayer1) _lobbyMenu.SetPlayer1Marked();
        else _lobbyMenu.SetPlayer2Marked();

        if(_lobbyMenu.LobbyFull())
        {
            //lobby just filled up. you can now challenge.
            if(lobbyPreviouslyEmpty)
                _lobbyMenu.SetChallengeState_NoChallenge();
        }
        //not enough players to challenge
        else
        {
            _lobbyMenu.SetChallengeState_CannotChallenge();
        }
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

    private void OnClientNewGameRequestSent()
    {
        _lobbyMenu.SetChallengeState_SentChallenge();
    }

    private void OnClientNewGameRequestReceived()
    {
        _lobbyMenu.SetChallengeState_GotChallenge();
    }

    private void OnClientNewGameAcceptSent()
    {
        _lobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientNewGameAcceptReceived()
    {
        _lobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientNewGameRejectSent()
    {
        _lobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameRejectReceived()
    {
        _lobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameCancelSent()
    {
        _lobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameCancelReceived()
    {
        _lobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientGameStarted(GameTurnEnum turn)
    {
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

    private void OnLobbyMenuChallengeSent()
    {
        _client.RequestNewGame();
    }

    private void OnLobbyMenuChallengeCanceled()
    {
        _client.CancelNewGame();
    }

    private void OnLobbyMenuChallengeAccepted()
    {
        _client.AcceptNewGame();
    }

    private void OnLobbyMenuChallengeRejected()
    {
        _client.RejectNewGame();
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
        _lobbyMenu.SetChallengeState_CannotChallenge();
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