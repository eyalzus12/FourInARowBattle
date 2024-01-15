using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// This is the UI class for the remote play menu
/// </summary>
public partial class RemotePlayMenu : Control
{
    public const string DISCONNECTING_STATUS = "Disconnecting...";
    public const string DISCONNECTED_STATUS = "Not connected.";
    public const string CONNECTING_STATUS = "Connecting...";
    public const string CONNECTED_STATUS = "Connected.";

    /// <summary>
    /// Server connect button pressed
    /// </summary>
    /// <param name="ip">The entered server ip</param>
    /// <param name="port">The entered server port</param>
    [Signal]
    public delegate void ServerConnectRequestedEventHandler(string ip, string port);
    /// <summary>
    /// Server connect cancel button pressed
    /// </summary>
    [Signal]
    public delegate void ServerConnectCancelRequestedEventHandler();
    /// <summary>
    /// Server disconnect button pressed
    /// </summary>
    [Signal]
    public delegate void ServerDisconnectRequestedEventHandler();
    /// <summary>
    /// Create lobby button pressed
    /// </summary>
    /// <param name="playerName">The entered player name</param>
    [Signal]
    public delegate void CreateLobbyRequestedEventHandler(string playerName);
    /// <summary>
    /// Join lobby button pressed
    /// </summary>
    /// <param name="lobbyId">The lobby id</param>
    /// <param name="playerName">The entered player name</param>
    [Signal]
    public delegate void JoinLobbyRequestedEventHandler(uint lobbyId, string playerName);
    /// <summary>
    /// Entered lobby number was invalid. Tells GameClientMenu to show an error.
    /// </summary>
    [Signal]
    public delegate void LobbyNumberWasInvalidEventHandler();
    /// <summary>
    /// Exit button pressed
    /// </summary>
    /// <param name="path">The path to the main menu scene</param>
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
        _createLobbyButton.Pressed += OnCreateLobbyButtonPressed;
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

    /// <summary>
    /// Event: Create lobby button pressed
    /// </summary>
    private void OnCreateLobbyButtonPressed()
    {
        EmitSignal(SignalName.CreateLobbyRequested, _playerNameField?.Text ?? "");
    }

    /// <summary>
    /// Event: Join lobby button pressed
    /// </summary>
    /// <param name="lobbyId">The lobby id</param>
    private void OnJoinLobbyButtonJoinLobbyButtonPressed(uint lobbyId)
    {
        EmitSignal(SignalName.JoinLobbyRequested, lobbyId, _playerNameField?.Text ?? "");
    }

    /// <summary>
    /// Event: Join lobby button pressed with invalid lobby number
    /// </summary>
    private void OnJoinLobbyButtonLobbyNumberWasInvalid()
    {
        EmitSignal(SignalName.LobbyNumberWasInvalid);
    }

    /// <summary>
    /// Event: Exit button pressed
    /// </summary>
    /// <param name="path">The path to the main menu scene</param>
    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
    }

    /// <summary>
    /// Event: Connect to server button pressed
    /// </summary>
    private void OnConnectToServerPressed()
    {
        EmitSignal(SignalName.ServerConnectRequested, _serverIP.Text, _serverPort.Text);
    }

    /// <summary>
    /// Event: Connect cancel button pressed
    /// </summary>
    private void OnCancelConnectPressed()
    {
        EmitSignal(SignalName.ServerConnectCancelRequested);
    }

    /// <summary>
    /// Event: Disconnect button pressed
    /// </summary>
    private void OnDisconnectFromServerPressed()
    {
        EmitSignal(SignalName.ServerDisconnectRequested);
    }

    #endregion

    /// <summary>
    /// Set to connecting state
    /// </summary>
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

    /// <summary>
    /// Set to connected state
    /// </summary>
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

    /// <summary>
    /// Set to disconnecting state
    /// </summary>
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

    /// <summary>
    /// Set to disconnected state
    /// </summary>
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
