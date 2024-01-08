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

    public override void _Ready()
    {
        #region Editor Errors

        //error for failure to set nodes in editor
        if(Client is null) { GD.PushError($"No {nameof(Client)} set"); return;}
        if(RemotePlayMenu is null) { GD.PushError($"No {nameof(RemotePlayMenu)} set"); return; }
        if(LobbyMenu is null) { GD.PushError($"No {nameof(LobbyMenu)} set"); return; }
        if(Game is null) { GD.PushError($"No {nameof(Game)} set"); return; }
        if(StatusLabel is null) { GD.PushError($"No {nameof(StatusLabel)} set"); return; }
        if(ErrorPopup is null) { GD.PushError($"No {nameof(ErrorPopup)} set"); return; }
        if(NoticePopup is null) { GD.PushError($"No {nameof(NoticePopup)} set"); return; }
        if(InitialState is null) { GD.PushError($"No {nameof(InitialState)} set"); return; }

        #endregion

        StatusLabel.Text = CONNECTING_STATUS;

        #region Signal Connecting

        GetWindow().SizeChanged += OnWindowSizeChanged;
        //VisiblityChanged is the only signal which handles all cases of closing the window
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

        #endregion
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
        DisplayError(description);
    }

    private void OnClientLobbyEntered(uint lobbyId, string? player1Name, string? player2Name, bool isPlayer1)
    {
        SwitchToLobbyMenu();
        LobbyMenu.SetLobbyId(lobbyId);
        LobbyMenu.SetPlayer1Name(player1Name ?? "");
        LobbyMenu.SetPlayer2Name(player2Name ?? "");
    }

    private void OnClientLobbyStateUpdated(string? player1Name, string? player2Name, bool isPlayer1)
    {
        LobbyMenu.SetPlayer1Name(player1Name ?? "");
        LobbyMenu.SetPlayer2Name(player2Name ?? "");
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

    }

    private void OnClientNewGameRequestReceived()
    {

    }

    private void OnClientNewGameAcceptSent()
    {

    }

    private void OnClientNewGameAcceptReceived()
    {

    }

    private void OnClientNewGameRejectSent()
    {

    }

    private void OnClientNewGameRejectReceived()
    {

    }

    private void OnClientNewGameCancelSent()
    {

    }

    private void OnClientNewGameCancelReceived()
    {

    }

    private void OnClientGameStarted()
    {

    }

    private void OnClientGameFinished()
    {

    }

    private void OnRemotePlayMenuCreateLobbyRequested(string playerName)
    {
        Client.ClientName = playerName;
        Client.CreateLobby();
    }

    private void OnRemotePlayMenuJoinLobbyRequested(uint id, string playerName)
    {
        Client.ClientName = playerName;
        Client.JoinLobby(id);
    }

    private void OnRemotePlayMenuGoBackRequested(string path)
    {
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnLobbyMenuExitLobbyRequested(string path)
    {
        if(path != RemotePlayMenu?.SceneFilePath)
        {
            GD.PushError($"Attempt to exit lobby into wrong scene {path}");
            return;
        }

        SwitchToRemotePlayMenu();
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
        if(!_inLobby)
            return;
        
        RemotePlayMenu.ProcessMode = ProcessModeEnum.Disabled;
        RemotePlayMenu.Visible = false;
        
        Game.ProcessMode = ProcessModeEnum.Disabled;
        Game.Visible = false;

        LobbyMenu.ProcessMode = ProcessModeEnum.Inherit;
        LobbyMenu.Visible = true;

        _inLobby = true;
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
    }

    public void DisplayError(string error)
    {
        if(!ErrorPopup.Visible)
        {
            ErrorPopup.DialogText = error;
            ErrorPopup.PopupCentered();
        }
    }

    public void DisplayNotice(string notice)
    {
        if(!NoticePopup.Visible)
        {
            NoticePopup.DialogText = notice;
            NoticePopup.PopupCentered();
        }
    }

    #endregion
}