using Godot;
using System;

namespace FourInARowBattle;

public partial class RemotePlayMenu : Control
{
    public const string DISCONNECTING_STATUS = "Disconnecting...";
    public const string DISCONNECTED_STATUS = "Not connected.";
    public const string CONNECTING_STATUS = "Connecting...";
    public const string CONNECTED_STATUS = "Connected.";

    [Signal]
    public delegate void ServerConnectRequestedEventHandler(string ip, string port);
    [Signal]
    public delegate void ServerConnectCancelRequestedEventHandler();
    [Signal]
    public delegate void ServerDisconnectRequestedEventHandler();
    [Signal]
    public delegate void CreateLobbyRequestedEventHandler(string playerName);
    [Signal]
    public delegate void JoinLobbyRequestedEventHandler(uint lobbyId, string playerName);
    [Signal]
    public delegate void LobbyNumberWasInvalidEventHandler();
    [Signal]
    public delegate void GoBackRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private Control _lobbyControlsBase = null!;
    [Export]
    private Button _createLobbyButton = null!;
    [Export]
    private JoinLobbyButton _joinLobbyButton = null!;
    [Export]
    private GoBackButton _goBackButton = null!;
    [Export]
    private LineEdit _playerNameField = null!;
    [Export]
    private LineEdit _serverIP = null!;
    [Export]
    private LineEdit _serverPort = null!;
    [Export]
    private Button _connectToServer = null!;
    [Export]
    private Button _cancelConnect = null!;
    [Export]
    private Button _disconnectFromServer = null!;
    [Export]
    private Label _connectingLabel = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_lobbyControlsBase);
        ArgumentNullException.ThrowIfNull(_createLobbyButton);
        ArgumentNullException.ThrowIfNull(_joinLobbyButton);
        ArgumentNullException.ThrowIfNull(_goBackButton);
        ArgumentNullException.ThrowIfNull(_playerNameField);
        ArgumentNullException.ThrowIfNull(_serverIP);
        ArgumentNullException.ThrowIfNull(_serverPort);
        ArgumentNullException.ThrowIfNull(_connectToServer);
        ArgumentNullException.ThrowIfNull(_cancelConnect);
        ArgumentNullException.ThrowIfNull(_disconnectFromServer);
        ArgumentNullException.ThrowIfNull(_connectingLabel);
    }

    private void ConnectSignals()
    {
        _createLobbyButton.Pressed += OnCreateLobbyButtonCreateLobbyButtonPressed;
        _joinLobbyButton.JoinLobbyButtonPressed += OnJoinLobbyButtonJoinLobbyButtonPressed;
        _joinLobbyButton.LobbyNumberWasInvalid += OnJoinLobbyButtonLobbyNumberWasInvalid;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        _connectToServer.Pressed += OnConnectToServerPressed;
        _cancelConnect.Pressed += OnCancelConnectPressed;
        _disconnectFromServer.Pressed += OnDisconnectFromServerPressed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        _playerNameField.MaxLength = Globals.NAME_LENGTH_LIMIT;
    }

    #region Signal Handling

    private void OnCreateLobbyButtonCreateLobbyButtonPressed()
    {
        EmitSignal(SignalName.CreateLobbyRequested, _playerNameField?.Text ?? "");
    }

    private void OnJoinLobbyButtonJoinLobbyButtonPressed(uint lobbyId)
    {
        EmitSignal(SignalName.JoinLobbyRequested, lobbyId, _playerNameField?.Text ?? "");
    }

    private void OnJoinLobbyButtonLobbyNumberWasInvalid()
    {
        EmitSignal(SignalName.LobbyNumberWasInvalid);
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
    }

    private void OnConnectToServerPressed()
    {
        EmitSignal(SignalName.ServerConnectRequested, _serverIP.Text, _serverPort.Text);
    }

    private void OnCancelConnectPressed()
    {
        EmitSignal(SignalName.ServerConnectCancelRequested);
    }

    private void OnDisconnectFromServerPressed()
    {
        EmitSignal(SignalName.ServerDisconnectRequested);
    }

    #endregion

    public void ShowAsConnecting()
    {
        _serverIP.Editable = false;
        _serverPort.Editable = false;
        _connectToServer.Visible = false;
        _connectToServer.Disabled = true;
        _cancelConnect.Visible = true;
        _cancelConnect.Disabled = false;
        _disconnectFromServer.Visible = false;
        _disconnectFromServer.Disabled = true;
        _connectingLabel.Text = CONNECTING_STATUS;
        _lobbyControlsBase.Visible = false;
    }

    public void ShowAsConnected()
    {
        _serverIP.Editable = false;
        _serverPort.Editable = false;
        _connectToServer.Visible = false;
        _connectToServer.Disabled = true;
        _cancelConnect.Visible = false;
        _cancelConnect.Disabled = true;
        _disconnectFromServer.Visible = true;
        _disconnectFromServer.Disabled = false;
        _connectingLabel.Text = CONNECTED_STATUS;
        _lobbyControlsBase.Visible = true;
    }

    public void ShowAsDisconnecting()
    {
        _serverIP.Editable = false;
        _serverPort.Editable = false;
        _connectToServer.Visible = true;
        _connectToServer.Disabled = true;
        _cancelConnect.Visible = false;
        _cancelConnect.Disabled = true;
        _disconnectFromServer.Visible = false;
        _disconnectFromServer.Disabled = true;
        _connectingLabel.Text = DISCONNECTING_STATUS;
        _lobbyControlsBase.Visible = false;
    }

    public void ShowAsDisconnected()
    {
        _serverIP.Editable = true;
        _serverPort.Editable = true;
        _connectToServer.Visible = true;
        _connectToServer.Disabled = false;
        _cancelConnect.Visible = false;
        _cancelConnect.Disabled = true;
        _disconnectFromServer.Visible = false;
        _disconnectFromServer.Disabled = true;
        _connectingLabel.Text = DISCONNECTED_STATUS;
        _lobbyControlsBase.Visible = false;
    }
}
