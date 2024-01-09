using System;
using Godot;

namespace FourInARowBattle;

public partial class GameClientMenu : Node
{
    public const string CONNECTING_STATUS = "Connecting... Please Wait.";
    public const string CONNECTED_STATUS = "Connected!";
    public const string DISCONNECTED_STATUS = "Disconnected. Please try again in a few minutes.";

    #region Editor-Set Values

    [Export]
    public GameClient Client{get; set;} = null!;

    [Export(PropertyHint.File, "*.tscn,*.scn")]
    public string MainMenu{get; set;} = "";
    [Export]
    public RemotePlayMenu RemotePlayMenu{get; set;} = null!;
    [Export]
    public LobbyMenu LobbyMenu{get; set;} = null!;
    [Export]
    public Game Game{get; set;} = null!;
    [Export]
    public Label StatusLabel{get; set;} = null!;
    [Export]
    public AcceptDialog NoticePopup{get; set;} = null!;
    [Export]
    public AcceptDialog ErrorPopup{get; set;} = null!;
    [Export]
    public GameData InitialState{get; set;} = null!;

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
        ArgumentNullException.ThrowIfNull(Game);
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
        Client.GameFinished += OnClientGameFinished;
        RemotePlayMenu.CreateLobbyRequested += OnRemotePlayMenuCreateLobbyRequested;
        RemotePlayMenu.JoinLobbyRequested += OnRemotePlayMenuJoinLobbyRequested;
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
    }

    private void OnNoticePopupClosed()
    {
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

    private void OnClientErrorOccured(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        DisplayError(description);
    }

    private void OnClientLobbyEntered(uint lobbyId, string? player1Name, string? player2Name, bool isPlayer1)
    {
        SwitchToLobbyMenu();
        LobbyMenu.SetLobbyId(lobbyId);
        LobbyMenu.SetPlayer1Name(player1Name ?? "");
        LobbyMenu.SetPlayer2Name(player2Name ?? "");

        if(LobbyMenu.LobbyFull())
            LobbyMenu.SetChallengeState_NoChallenge();
        else
            LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void OnClientLobbyStateUpdated(string? player1Name, string? player2Name, bool isPlayer1)
    {
        bool lobbyPreviouslyEmpty = !LobbyMenu.LobbyFull();
        LobbyMenu.SetPlayer1Name(player1Name ?? "");
        LobbyMenu.SetPlayer2Name(player2Name ?? "");
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
    }

    private void OnClientGameFinished()
    {
        SwitchToLobbyMenu();
    }

    private void OnRemotePlayMenuCreateLobbyRequested(string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        Client.ClientName = playerName;
        Client.CreateLobby();
    }

    private void OnRemotePlayMenuJoinLobbyRequested(uint id, string playerName)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        Client.ClientName = playerName;
        Client.JoinLobby(id);
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

        Game.ProcessMode = ProcessModeEnum.Disabled;
        Game.Visible = false;

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
        
        Game.ProcessMode = ProcessModeEnum.Disabled;
        Game.Visible = false;

        LobbyMenu.ProcessMode = ProcessModeEnum.Inherit;
        LobbyMenu.Visible = true;

        _inLobby = true;
        LobbyMenu.SetChallengeState_CannotChallenge();
    }

    private void SwitchToGame()
    {
        RemotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        RemotePlayMenu.Visible = false;

        LobbyMenu.ProcessMode = ProcessModeEnum.Disabled;
        LobbyMenu.Visible = false;
        
        Game.ProcessMode = ProcessModeEnum.Inherit;
        Game.Visible = true;

        Game.DeserializeFrom(InitialState);

        _inGame = true;
    }

    public void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!ErrorPopup.Visible)
        {
            ErrorPopup.DialogText = error;
            ErrorPopup.PopupCentered();
        }
    }

    public void DisplayNotice(string notice)
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