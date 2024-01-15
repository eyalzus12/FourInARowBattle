using System;
using System.Linq;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// This class is the high-level UI for the client.
/// It takes input from the user and gives it to GameClient for handling and sending to the server.
/// It takes the server responses from GameClient and displays it for the user.
/// </summary>
public partial class GameClientMenu : Node
{
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
    private AcceptDialog _noticePopup = null!;
    [Export]
    private AcceptDialog _errorPopup = null!;
    [ExportCategory("")]
    [Export]
    private GameData _initialState = null!;

    #endregion

    private bool _waitingToDisconnect = false;
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
        _client.GameQuitByOpponent += OnClientGameQuitByOpponent;
        _client.GameQuitBySelf += OnClientGameQuitBySelf;
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
        _remotePlayMenu.ServerConnectRequested += OnRemotePlayMenuServerConnectRequested;
        _remotePlayMenu.ServerConnectCancelRequested += OnRemotePlayMenuServerConnectCancelRequested;
        _remotePlayMenu.ServerDisconnectRequested += OnRemotePlayMenuServerDisconnectRequested;
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
        _gameMenu.GameQuitRequested += OnGameMenuGameQuitRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();

        _remotePlayMenu.ShowAsDisconnected();
        SwitchToRemotePlayMenu();
    }


    #region Signal Handling

    /// <summary>
    /// Event: Window size changed. Re-center popups.
    /// </summary>
    private void OnWindowSizeChanged()
    {
        if(_errorPopup.Visible)
            _errorPopup.PopupCentered();
        if(_noticePopup.Visible)
            _noticePopup.PopupCentered();
    }

    /// <summary>
    /// Event: Error popup was closed
    /// </summary>
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

    /// <summary>
    /// Event: Notice popup was closed
    /// </summary>
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

    /// <summary>
    /// Event: Client connected
    /// </summary>
    private void OnClientConnected()
    {
        GD.Print("Connected!");
        _remotePlayMenu.ShowAsConnected();
    }

    /// <summary>
    /// Event: Client disconnected
    /// </summary>
    private void OnClientDisconnected()
    {
        GD.Print("Connection closed");

        //disconnect was not expected
        if(!_waitingToDisconnect)
        {
            DisplayError("Connection failed");
        }

        SwitchToRemotePlayMenu();
        _remotePlayMenu.ShowAsDisconnected();
        _waitingToDisconnect = false;
    }

    /// <summary>
    /// Event: Server closed
    /// </summary>
    private void OnClientServerClosed()
    {
        GD.Print("Server closed");
        DisplayNotice("Server Closed!");
        SwitchToRemotePlayMenu();
        _remotePlayMenu.ShowAsDisconnecting();
        _waitingToDisconnect = true;
    }

    /// <summary>
    /// Event: GameClient had an error
    /// </summary>
    /// <param name="description">Error description</param>
    private void OnClientErrorOccured(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        DisplayError(description);
    }

    /// <summary>
    /// Event: Entered lobby
    /// </summary>
    /// <param name="lobbyId">The lobby id</param>
    /// <param name="players">The data of the players in the lobby</param>
    /// <param name="index">Our index inside the lobby</param>
    private void OnClientLobbyEntered(uint lobbyId, LobbyPlayerData[] players, int index)
    {
        SwitchToLobbyMenu();
        _lobbyMenu.SetLobbyId(lobbyId);
        string[] names = players.Select(p => p.Name).ToArray();
        _lobbyMenu.SetPlayerNames(names);
        _lobbyMenu.SetMark(index);
        for(int i = 0; i < players.Length; ++i)
        {
            //cannot challenge yourself
            if(i == index)
                _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, i);
            //cannot challenge busy player
            else if(players[i].Busy)
                _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, i);
            //can challenge non-busy player
            else
                _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, i);
        }
    }

    /// <summary>
    /// Event: New player joined lobby
    /// </summary>
    /// <param name="name">The player name</param>
    private void OnClientLobbyPlayerJoined(string name)
    {
        _lobbyMenu.AddPlayer(name);
    }

    /// <summary>
    /// Event: Player left lobby
    /// </summary>
    /// <param name="index">The index of the leaving player</param>
    private void OnClientLobbyPlayerLeft(int index)
    {
        _lobbyMenu.RemovePlayer(index);
    }

    /// <summary>
    /// Event: Show timeout warning
    /// </summary>
    /// <param name="secondsRemaining">Seconds remaining before timeout</param>
    private void OnClientLobbyTimeoutWarned(int secondsRemaining)
    {
        DisplayNotice($"Lobby will timeout in {secondsRemaining} seconds");
    }

    /// <summary>
    /// Event: Server timed out. Show notice and kick to remote play menu.
    /// </summary>
    private void OnClientLobbyTimedOut()
    {
        DisplayNotice("Lobby timed out");
        _kickingToRemotePlayMenu = true;
    }

    /// <summary>
    /// Event: Opponent quit game. Show notice and kick to lobby.
    /// </summary>
    private void OnClientGameQuitByOpponent()
    {
        DisplayNotice("Opponent quit");
        _kickingToLobby = true;
    }

    /// <summary>
    /// Event: We quit game. Kick to lobby.
    /// </summary>
    private void OnClientGameQuitBySelf()
    {
        SwitchToLobbyMenu();
    }

    /// <summary>
    /// Event: Succesfully sent game request
    /// </summary>
    /// <param name="playerIndex">Who the request was for</param>
    private void OnClientNewGameRequestSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.SENT, playerIndex);
    }

    /// <summary>
    /// Event: Got game request
    /// </summary>
    /// <param name="playerIndex">Who sent the request</param>
    private void OnClientNewGameRequestReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.GOT, playerIndex);
    }

    /// <summary>
    /// Event: Succesfully sent request approval
    /// </summary>
    /// <param name="playerIndex">Who sent the request</param>
    private void OnClientNewGameAcceptSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    /// <summary>
    /// Event: Game request was approved
    /// </summary>
    /// <param name="playerIndex">Who approved the request</param>
    private void OnClientNewGameAcceptReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    /// <summary>
    /// Event: Succesfuly sent request rejection
    /// </summary>
    /// <param name="playerIndex">Who sent the request</param>
    private void OnClientNewGameRejectSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    /// <summary>
    /// Event: Game request was rejected
    /// </summary>
    /// <param name="playerIndex">Who rejected the request</param>
    private void OnClientNewGameRejectReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    /// <summary>
    /// Event: Succesfuly sent request cancelation
    /// </summary>
    /// <param name="playerIndex">Who the request was for</param>
    private void OnClientNewGameCancelSent(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    /// <summary>
    /// Event: Got request cancelation
    /// </summary>
    /// <param name="playerIndex">Who canceled the request</param>
    private void OnClientNewGameCancelReceived(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    /// <summary>
    /// Event: Player got into a game
    /// </summary>
    /// <param name="playerIndex">Who got into a game</param>
    private void OnClientPlayerBecameBusy(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.CANNOT, playerIndex);
    }

    /// <summary>
    /// Event: Player is no longer in a game
    /// </summary>
    /// <param name="playerIndex">Who left the game</param>
    private void OnClientPlayerBecameAvailable(int playerIndex)
    {
        _lobbyMenu.SetChallengeState(ChallengeStateEnum.NONE, playerIndex);
    }

    /// <summary>
    /// Event: Game started. Initialize game.
    /// </summary>
    /// <param name="turn">Our turn</param>
    /// <param name="opponentIndex">The opponent</param>
    private void OnClientGameStarted(GameTurnEnum turn, int opponentIndex)
    {
        _lobbyMenu.SetAllChallengeStatesExceptMark(ChallengeStateEnum.NONE);
        SwitchToGame();
        string me = _lobbyMenu.GetMarkedName();
        string opponent = _lobbyMenu.GetPlayerName(opponentIndex);
        bool imPlayer1 = turn == GameTurnEnum.PLAYER1;
        string player1 = imPlayer1 ? me : opponent;
        string player2 = imPlayer1 ? opponent : me;
        _gameMenu.SetPlayers(player1, player2, imPlayer1);
        _gameMenu.AllowedTurns = new(){turn};
        _gameMenu.InitGame();
    }

    /// <summary>
    /// Event: Succesfuly placed token
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="scene">The token scene</param>
    private void OnClientGameActionPlaceSent(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _gameMenu.PlaceToken(column, scene);
    }

    /// <summary>
    /// Event: Opponent placed a token
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="scene">The token scene</param>
    private void OnClientGameActionPlaceReceived(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ErrorCodeEnum? err = _gameMenu.PlaceToken(column, scene);
        //we do desync check here because GameClient does not directly interact with the game
        if(err is not null)
        {
            GD.Print($"Other player's placement produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            _client.Desync();
            return;
        }
    }

    /// <summary>
    /// Event: Succesfuly refilled
    /// </summary>
    private void OnClientGameActionRefillSent()
    {
        _gameMenu.Refill();
    }

    /// <summary>
    /// Event: Opponent refilled
    /// </summary>
    private void OnClientGameActionRefillReceived()
    {
        ErrorCodeEnum? err = _gameMenu.Refill();
        //we do desync check here because GameClient does not directly interact with the game
        if(err is not null)
        {
            GD.Print($"Other player's refill produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            _client.Desync();
            return;
        }
    }

    /// <summary>
    /// Event: Game finished
    /// </summary>
    private void OnClientGameFinished()
    {
        SwitchToLobbyMenu();
    }

    /// <summary>
    /// Event: Server connection button pressed
    /// </summary>
    /// <param name="ip">The entered server ip</param>
    /// <param name="port">The entered server port</param>
    private void OnRemotePlayMenuServerConnectRequested(string ip, string port)
    {
        ArgumentNullException.ThrowIfNull(ip);
        ArgumentNullException.ThrowIfNull(port);
        //if there's an error here, GameClient displays it
        Error err = _client.ConnectToServer(ip, port);
        if(err == Error.Ok)
            _remotePlayMenu.ShowAsConnecting();
    }

    /// <summary>
    /// Event: Server connection cancel button pressed
    /// </summary>
    private void OnRemotePlayMenuServerConnectCancelRequested()
    {
        _client.CloseConnection();
        _remotePlayMenu.ShowAsDisconnecting();
        _waitingToDisconnect = true;
    }

    /// <summary>
    /// Event: Server disconnect button pressed
    /// </summary>
    private void OnRemotePlayMenuServerDisconnectRequested()
    {
        _client.DisconnectFromServer(DisconnectReasonEnum.DESIRE);
        _remotePlayMenu.ShowAsDisconnecting();
        _waitingToDisconnect = true;
    }

    /// <summary>
    /// Event: Lobby creation button pressed
    /// </summary>
    /// <param name="playerName">The entered player name</param>
    private void OnRemotePlayMenuCreateLobbyRequested(string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        if(playerName == "") playerName = "Guest";
        _client.ClientName = playerName;
        _client.CreateLobby();
    }

    /// <summary>
    /// Event: Lobby joining button pressed
    /// </summary>
    /// <param name="id">The entered lobby id</param>
    /// <param name="playerName">The entered player name</param>
    private void OnRemotePlayMenuJoinLobbyRequested(uint id, string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        if(playerName == "") playerName = "Guest";
        _client.ClientName = playerName;
        _client.JoinLobby(id);
    }

    /// <summary>
    /// Event: Lobby joining button pressed with an invalid lobby number
    /// </summary>
    private void OnRemotePlayMenuLobbyNumberWasInvalid()
    {
        DisplayError("Invalid lobby number");
    }

    /// <summary>
    /// Event: Go back to main menu button pressed
    /// </summary>
    /// <param name="path">The path to the main menu scene</param>
    private void OnRemotePlayMenuGoBackRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    /// <summary>
    /// Event: Exit lobby button pressed and confirmed
    /// </summary>
    /// <param name="path">The path to the remote play menu scene</param>
    private void OnLobbyMenuExitLobbyRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        //wrong scene path
        if(path != _remotePlayMenu.SceneFilePath)
        {
            GD.PushError($"Attempt to exit lobby into wrong scene {path}");
            return;
        }

        SwitchToRemotePlayMenu();
    }

    /// <summary>
    /// Event: Game request button pressed
    /// </summary>
    /// <param name="index">The player it was pressed for</param>
    private void OnLobbyMenuChallengeSent(int index)
    {
        _client.RequestNewGame(index);
    }

    /// <summary>
    /// Event: Game request cancel button pressed
    /// </summary>
    /// <param name="index">The player is was pressed for</param>
    private void OnLobbyMenuChallengeCanceled(int index)
    {
        _client.CancelNewGame(index);
    }

    /// <summary>
    /// Event: Game request accept button pressed
    /// </summary>
    /// <param name="index">The player it was pressed for</param>
    private void OnLobbyMenuChallengeAccepted(int index)
    {
        _client.AcceptNewGame(index);
    }

    /// <summary>
    /// Event: Game request reject button pressed
    /// </summary>
    /// <param name="index">The player it was pressed for</param>
    private void OnLobbyMenuChallengeRejected(int index)
    {
        _client.RejectNewGame(index);
    }

    /// <summary>
    /// Event: A token was placed by us. Send to server for verification. OnClientGameActionPlaceSent will be called when verified.
    /// </summary>
    /// <param name="column">The column it was placed</param>
    /// <param name="token">The token scene</param>
    private void OnGameMenuTokenPlaceAttempted(int column, PackedScene token)
    {
        _client.PlaceToken((byte)column, token.ResourcePath);
    }

    /// <summary>
    /// Event: The refill button was pressed. Send to server for verification. OnClientGameActionRefillSent will be called when verified.
    /// </summary>
    private void OnGameMenuRefillAttempted()
    {
        _client.Refill();
    }

    /// <summary>
    /// Event: Game quit button pressed and confirmed. Kick to lobby.
    /// </summary>
    /// <param name="path">The path to the lobby scene</param>
    private void OnGameMenuGameQuitRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        //wrong scene
        if(path != _lobbyMenu.SceneFilePath)
        {
            GD.PushError($"Attempt to exit lobby into wrong scene {path}");
            return;
        }
        _client.QuitGame();
    }

    #endregion
    
    #region Menu Operations

    /// <summary>
    /// Do the needed operations when switching into the remote play menu
    /// </summary>
    private void HandleRemotePlayMenuEnter()
    {
        _remotePlayMenu.ProcessMode = ProcessModeEnum.Inherit;
        _remotePlayMenu.Visible = true;
    }

    /// <summary>
    /// Do the needed operations when switching out of the remote play menu
    /// </summary>
    private void HandleRemotePlayMenuExit()
    {
        _remotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        _remotePlayMenu.Visible = false;
    }

    /// <summary>
    /// Do the needed operations when switching into the lobby
    /// </summary>
    private void HandleLobbyEnter()
    {
        //lobby is already active screen
        if(_inLobby && !_inGame) return;
        _inLobby = true;
        _lobbyMenu.ProcessMode = ProcessModeEnum.Inherit;
        _lobbyMenu.Visible = true;
    }

    /// <summary>
    /// Do the needed operations when switching out of the lobby
    /// </summary>
    /// <param name="disconnect">Whether to disconnect. True when switching into remote play menu, False when switching into game.</param>
    private void HandleLobbyExit(bool disconnect = false)
    {
        if(!_inLobby) return;

        if(disconnect)
        {
            _client.DisconnectFromLobby(DisconnectReasonEnum.DESIRE);
            _inLobby = false;
            _lobbyMenu.ClearPlayers();
        }

        _lobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        _lobbyMenu.Visible = false;
    }

    /// <summary>
    /// Do the needed operations when switching into the game
    /// </summary>
    private void HandleGameEnter()
    {
        if(_inGame) return;
        _inGame = true;
        _gameMenu.ProcessMode = ProcessModeEnum.Inherit;
        _gameMenu.Visible = true;
        _gameMenu.DeserializeFrom(_initialState);
    }

    /// <summary>
    /// Do the needed operations when switching out of the game
    /// </summary>
    private void HandleGameExit()
    {
        if(!_inGame) return;
        _inGame = false;
        _gameMenu.ProcessMode = ProcessModeEnum.Disabled;
        _gameMenu.Visible = false;
    }

    /// <summary>
    /// Switch to the remote play menu
    /// </summary>
    private void SwitchToRemotePlayMenu()
    {
        HandleRemotePlayMenuEnter();
        HandleGameExit();
        HandleLobbyExit(true);
    }

    /// <summary>
    /// Switch to the lobby menu
    /// </summary>
    private void SwitchToLobbyMenu()
    {
        HandleLobbyEnter();
        HandleGameExit();
        HandleRemotePlayMenuExit();
    }

    /// <summary>
    /// Switch to the game
    /// </summary>
    private void SwitchToGame()
    {
        HandleGameEnter();
        HandleRemotePlayMenuExit();
        HandleLobbyExit();
    }

    /// <summary>
    /// Display an error to the screen
    /// </summary>
    /// <param name="error">The error to display</param>
    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!_errorPopup.Visible)
        {
            _errorPopup.DialogText = error;
            _errorPopup.PopupCentered();
        }
    }

    /// <summary>
    /// Display a notice to the screen
    /// </summary>
    /// <param name="notice">The notice to display</param>
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