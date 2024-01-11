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
    private GameClient Client = null!;
    [Export(PropertyHint.File, "*.tscn,*.scn")]
    private string MainMenu = "";
    [Export]
    private RemotePlayMenu RemotePlayMenu = null!;
    [Export]
    private LobbyMenu LobbyMenu = null!;
    [Export]
    private GameMenu GameMenu = null!;
    [Export]
    private Label StatusLabel = null!;
    [Export]
    private AcceptDialog NoticePopup = null!;
    [Export]
    private AcceptDialog ErrorPopup = null!;
    [ExportCategory("")]
    [Export]
    private GameData InitialState = null!;

    #endregion

    private bool _inGame = false;
    private bool _inLobby = false;
    private bool _kickingToMainMenu = false;
    private bool _kickingToRemotePlayMenu = false;
    private bool _kickingToLobby = false;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Client);
        ArgumentNullException.ThrowIfNull(RemotePlayMenu);
        ArgumentNullException.ThrowIfNull(LobbyMenu);
        ArgumentNullException.ThrowIfNull(GameMenu);
        ArgumentNullException.ThrowIfNull(StatusLabel);
        ArgumentNullException.ThrowIfNull(ErrorPopup);
        ArgumentNullException.ThrowIfNull(NoticePopup);
        ArgumentNullException.ThrowIfNull(InitialState);
    }

    private void ConnectSignals()
    {
        GetWindow().SizeChanged += OnWindowSizeChanged;
        ErrorPopup.VisibilityChanged += OnErrorPopupClosed;
        NoticePopup.VisibilityChanged += OnNoticePopupClosed;
        Client.Connected += OnClientConnected;
        Client.Disconnected += OnClientDisconnected;
        Client.ServerClosed += OnClientServerClosed;
        Client.ErrorOccured += OnClientErrorOccured;
        Client.LobbyEntered += OnClientLobbyEntered;
        Client.LobbyStateUpdated += OnClientLobbyStateUpdated;
        Client.LobbyTimeoutWarned += OnClientLobbyTimeoutWarned;
        Client.LobbyTimedOut += OnClientLobbyTimedOut;
        Client.GameEjected += OnClientGameEjected;
        Client.NewGameRequestSent += OnClientNewGameRequestSent;
        Client.NewGameRequestReceived += OnClientNewGameRequestReceived;
        Client.NewGameAcceptSent += OnClientNewGameAcceptSent;
        Client.NewGameAcceptReceived += OnClientNewGameAcceptReceived;
        Client.NewGameRejectSent += OnClientNewGameRejectSent;
        Client.NewGameRejectReceived += OnClientNewGameRejectReceived;
        Client.NewGameCancelSent += OnClientNewGameCancelSent;
        Client.NewGameCancelReceived += OnClientNewGameCancelReceived;
        Client.GameStarted += OnClientGameStarted;
        Client.GameActionPlaceSent += OnClientGameActionPlaceSent;
        Client.GameActionPlaceReceived += OnClientGameActionPlaceReceived;
        Client.GameActionRefillSent += OnClientGameActionRefillSent;
        Client.GameActionRefillReceived += OnClientGameActionRefillReceived;
        Client.GameFinished += OnClientGameFinished;
        RemotePlayMenu.CreateLobbyRequested += OnRemotePlayMenuCreateLobbyRequested;
        RemotePlayMenu.JoinLobbyRequested += OnRemotePlayMenuJoinLobbyRequested;
        RemotePlayMenu.LobbyNumberWasInvalid += OnRemotePlayMenuLobbyNumberWasInvalid;
        RemotePlayMenu.GoBackRequested += OnRemotePlayMenuGoBackRequested;
        LobbyMenu.ExitLobbyRequested += OnLobbyMenuExitLobbyRequested;
        LobbyMenu.ChallengeSent += OnLobbyMenuChallengeSent;
        LobbyMenu.ChallengeCanceled += OnLobbyMenuChallengeCanceled;
        LobbyMenu.ChallengeAccepted += OnLobbyMenuChallengeAccepted;
        LobbyMenu.ChallengeRejected += OnLobbyMenuChallengeRejected;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        StatusLabel.Text = CONNECTING_STATUS;
    }


    #region Signal Handling

    private void OnWindowSizeChanged()
    {
        if(ErrorPopup.Visible)
            ErrorPopup.PopupCentered();
    }

    private void OnErrorPopupClosed()
    {
        if(_kickingToMainMenu)
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenu);
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
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenu);
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

        StatusLabel.Text = CONNECTED_STATUS;
    }

    private void OnClientDisconnected()
    {
        GD.Print("Connection closed");
        DisplayError("Connection failed");
        _kickingToMainMenu = true;

        StatusLabel.Text = DISCONNECTED_STATUS;
    }

    private void OnClientServerClosed()
    {
        GD.Print("Server closed");
        _kickingToMainMenu = true;
        DisplayNotice("Server Closed!");
        Client.CloseConnection();

        StatusLabel.Text = DISCONNECTED_STATUS;
    }

    private void OnClientErrorOccured(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        DisplayError(description);
    }

    private void OnClientLobbyEntered(uint lobbyId, string? player1Name, string? player2Name, bool isPlayer1)
    {
        SwitchToLobbyMenu();
        LobbyMenu.SetLobbyId(lobbyId);
        LobbyMenu.SetPlayer1Name(player1Name ?? "Guest");
        LobbyMenu.SetPlayer2Name(player2Name ?? "Guest");

        if(isPlayer1) LobbyMenu.SetPlayer1Marked();
        else LobbyMenu.SetPlayer2Marked();

        if(LobbyMenu.LobbyFull())
            LobbyMenu.SetChallengeState_NoChallenge();
        else
            LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientLobbyStateUpdated(string? player1Name, string? player2Name, bool isPlayer1)
    {
        bool lobbyPreviouslyEmpty = !LobbyMenu.LobbyFull();
        LobbyMenu.SetPlayer1Name(player1Name ?? "Guest");
        LobbyMenu.SetPlayer2Name(player2Name ?? "Guest");

        if(isPlayer1) LobbyMenu.SetPlayer1Marked();
        else LobbyMenu.SetPlayer2Marked();

        if(LobbyMenu.LobbyFull())
        {
            //lobby just filled up. you can now challenge.
            if(lobbyPreviouslyEmpty)
                LobbyMenu.SetChallengeState_NoChallenge();
        }
        //not enough players to challenge
        else
        {
            LobbyMenu.SetChallengeState_CannotChallenge();
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
        _kickingToRemotePlayMenu = true;
    }

    private void OnClientNewGameRequestSent()
    {
        LobbyMenu.SetChallengeState_SentChallenge();
    }

    private void OnClientNewGameRequestReceived()
    {
        LobbyMenu.SetChallengeState_GotChallenge();
    }

    private void OnClientNewGameAcceptSent()
    {
        LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientNewGameAcceptReceived()
    {
        LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientNewGameRejectSent()
    {
        LobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameRejectReceived()
    {
        LobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameCancelSent()
    {
        LobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientNewGameCancelReceived()
    {
        LobbyMenu.SetChallengeState_NoChallenge();
    }

    private void OnClientGameStarted(GameTurnEnum turn)
    {
        SwitchToGame();
        GameMenu.AllowedTurns = new(){turn};
    }

    private void OnClientGameActionPlaceSent(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        GameMenu.PlaceToken(column, scene);
    }

    private void OnClientGameActionPlaceReceived(int column, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ErrorCodeEnum? err = GameMenu.PlaceToken(column, scene);
        if(err is not null)
        {
            GD.Print($"Other player's placement produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            Client.Desync();
            return;
        }
    }

    private void OnClientGameActionRefillSent()
    {
        GameMenu.Refill();
    }

    private void OnClientGameActionRefillReceived()
    {
        ErrorCodeEnum? err = GameMenu.Refill();
        if(err is not null)
        {
            GD.Print($"Other player's refill produced error {ErrorCodeUtils.Humanize((ErrorCodeEnum)err)}??");
            Client.Desync();
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
        Client.ClientName = playerName;
        Client.CreateLobby();
    }

    private void OnRemotePlayMenuJoinLobbyRequested(uint id, string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        if(playerName == "") playerName = "Guest";
        Client.ClientName = playerName;
        Client.JoinLobby(id);
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
        if(path != RemotePlayMenu.SceneFilePath)
        {
            GD.PushError($"Attempt to exit lobby into wrong scene {path}");
            return;
        }

        SwitchToRemotePlayMenu();
    }

    private void OnLobbyMenuChallengeSent()
    {
        Client.RequestNewGame();
    }

    private void OnLobbyMenuChallengeCanceled()
    {
        Client.CancelNewGame();
    }

    private void OnLobbyMenuChallengeAccepted()
    {
        Client.AcceptNewGame();
    }

    private void OnLobbyMenuChallengeRejected()
    {
        Client.RejectNewGame();
    }

    #endregion
    
    #region Menu Operations

    private void SwitchToRemotePlayMenu()
    {
        LobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        LobbyMenu.Visible = false;

        GameMenu.ProcessMode = ProcessModeEnum.Disabled;
        GameMenu.Visible = false;

        _inGame = false;
        if(_inLobby)
            Client.DisconnectFromLobby(DisconnectReasonEnum.DESIRE);

        RemotePlayMenu.ProcessMode = ProcessModeEnum.Inherit;
        RemotePlayMenu.Visible = true;
    }

    private void SwitchToLobbyMenu()
    {
        RemotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        RemotePlayMenu.Visible = false;
        
        GameMenu.ProcessMode = ProcessModeEnum.Disabled;
        GameMenu.Visible = false;

        LobbyMenu.ProcessMode = ProcessModeEnum.Inherit;
        LobbyMenu.Visible = true;
        LobbyMenu.ClearMark();

        _inLobby = true;
        LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void SwitchToGame()
    {
        RemotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        RemotePlayMenu.Visible = false;

        LobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        LobbyMenu.Visible = false;
        
        GameMenu.ProcessMode = ProcessModeEnum.Inherit;
        GameMenu.Visible = true;

        GameMenu.Game.DeserializeFrom(InitialState);

        _inGame = true;
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!ErrorPopup.Visible)
        {
            ErrorPopup.DialogText = error;
            ErrorPopup.PopupCentered();
        }
    }

    private void DisplayNotice(string notice)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if(!NoticePopup.Visible)
        {
            NoticePopup.DialogText = notice;
            NoticePopup.PopupCentered();
        }
    }

    #endregion
}