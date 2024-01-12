using Godot;
using DequeNet;
using System;

namespace FourInARowBattle;

public partial class GameClient : Node
{
    public const string CONNECTION_URL = "127.0.0.1:1234";

    #region Signals

    [Signal]
    public delegate void ConnectedEventHandler();
    [Signal]
    public delegate void DisconnectedEventHandler();
    [Signal]
    public delegate void ServerClosedEventHandler();
    [Signal]
    public delegate void ErrorOccuredEventHandler(string description);
    [Signal]
    public delegate void LobbyEnteredEventHandler(uint lobbyId, string? player1Name, string? player2Name, bool isPlayer1);
    [Signal]
    public delegate void LobbyStateUpdatedEventHandler(string? player1Name, string? player2Name, bool isPlayer1);
    [Signal]
    public delegate void LobbyTimeoutWarnedEventHandler(int secondsRemaining);
    [Signal]
    public delegate void LobbyTimedOutEventHandler();
    [Signal]
    public delegate void GameEjectedEventHandler();
    [Signal]
    public delegate void NewGameRequestSentEventHandler();
    [Signal]
    public delegate void NewGameRequestReceivedEventHandler();
    [Signal]
    public delegate void NewGameAcceptSentEventHandler();
    [Signal]
    public delegate void NewGameAcceptReceivedEventHandler();
    [Signal]
    public delegate void NewGameRejectSentEventHandler();
    [Signal]
    public delegate void NewGameRejectReceivedEventHandler();
    [Signal]
    public delegate void NewGameCancelSentEventHandler();
    [Signal]
    public delegate void NewGameCancelReceivedEventHandler();
    [Signal]
    public delegate void GameStartedEventHandler(GameTurnEnum turn);
    [Signal]
    public delegate void GameActionPlaceSentEventHandler(int column, PackedScene scene);
    [Signal]
    public delegate void GameActionPlaceReceivedEventHandler(int column, PackedScene scene);
    [Signal]
    public delegate void GameActionRefillSentEventHandler();
    [Signal]
    public delegate void GameActionRefillReceivedEventHandler();
    [Signal]
    public delegate void GameFinishedEventHandler();

    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private WebSocketClient _client = null!;

    private readonly Deque<byte> _buffer = new();

    public string ClientName{get; set;} = "";

    #region State Variables

    //curent lobby
    private uint? _lobby = null;
    private bool? _isPlayer1 = null;
    //lobby connect request
    private Packet_ConnectLobbyRequest? _lobbyConnectionPacket = null;
    //name of other player
    private string? _otherPlayer = null;

    private Packet_NewGameRequest? _gameRequestPacket = null;
    private Packet_NewGameAccept? _gameAcceptPacket = null;
    private Packet_NewGameReject? _gameRejectPacket = null;
    private Packet_NewGameCancel? _gameCancelPacket = null;

    private bool _iHaveRequest = false;
    private bool _otherPlayerHasRequest = false;

    private bool _gameShouldStart = false;
    private bool _inGame = false;

    private Packet_GameActionPlace? _placePacket = null;
    private Packet_GameActionRefill? _refillPacket = null;

    #endregion

    public Game? Game{get; set;} = null;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_client);
    }

    private void ConnectSignals()
    {
        _client.PacketReceived += OnWebSocketClientPacketReceived;
        _client.ConnectedToServer += OnWebSocketClientConnected;
        _client.ConnectionClosed += OnWebSocketClientConnectionClosed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        Error err = _client.ConnectToUrl(CONNECTION_URL);
        if(err != Error.Ok)
        {
            DisplayError($"Error while trying to connect: {err}");
        }
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            CloseConnection();
        }
    }

    #region Signal Handling

    private void OnWebSocketClientPacketReceived(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        _buffer.PushRightRange(packetBytes);

        while(_buffer.Count > 0 && AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(packet);
        }
    }

    private void OnWebSocketClientConnected()
    {
        EmitSignal(SignalName.Connected);
    }

    private void OnWebSocketClientConnectionClosed()
    {
        CloseConnection();
        EmitSignal(SignalName.Disconnected);
    }

    #endregion

    #region Packet Handling

    public void SendPacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if(_client.State != WebSocketPeer.State.Open)
        {
            DisplayError("Connection to server is not yet established. Please wait.");
            return;
        }

        Error err = _client.SendPacket(packet.ToByteArray());
        if(err != Error.Ok)
            DisplayError($"Error {err} while trying to communicate with server");
    }

    public void HandlePacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        //giant switch statement to handle all packets
        //can't use polymorphism since that would require to expose the GameClient privates to the packets.
        //and can't use a dictionary since we need to cast the packets (and doing so generically would require reflection)
        switch(packet)
        {
            case Packet_Dummy _packet:
                HandlePacket_Dummy(_packet);
                break;
            case Packet_InvalidPacket _packet:
                HandlePacket_InvalidPacket(_packet);
                break;
            case Packet_InvalidPacketInform _packet:
                HandlePacket_InvalidPacketInform(_packet);
                break;
            case Packet_CreateLobbyOk _packet:
                HandlePacket_CreateLobbyOk(_packet);
                break;
            case Packet_CreateLobbyFail _packet:
                HandlePacket_CreateLobbyFail(_packet);
                break;
            case Packet_ConnectLobbyOk _packet:
                HandlePacket_ConnectLobbyOk(_packet);
                break;
            case Packet_ConnectLobbyFail _packet:
                HandlePacket_ConnectLobbyFail(_packet);
                break;
            case Packet_LobbyNewPlayer _packet:
                HandlePacket_LobbyNewPlayer(_packet);
                break;
            case Packet_NewGameRequestOk _packet:
                HandlePacket_NewGameRequestOk(_packet);
                break;
            case Packet_NewGameRequestFail _packet:
                HandlePacket_NewGameRequestFail(_packet);
                break;
            case Packet_NewGameRequested _packet:
                HandlePacket_NewGameRequested(_packet);
                break;
            case Packet_NewGameAcceptOk _packet:
                HandlePacket_NewGameAcceptOk(_packet);
                break;
            case Packet_NewGameAcceptFail _packet:
                HandlePacket_NewGameAcceptFail(_packet);
                break;
            case Packet_NewGameAccepted _packet:
                HandlePacket_NewGameAccepted(_packet);
                break;
            case Packet_NewGameRejectOk _packet:
                HandlePacket_NewGameRejectOk(_packet);
                break;
            case Packet_NewGameRejectFail _packet:
                HandlePacket_NewGameRejectFail(_packet);
                break;
            case Packet_NewGameRejected _packet:
                HandlePacket_NewGameRejected(_packet);
                break;
            case Packet_NewGameCancelOk _packet:
                HandlePacket_NewGameCancelOk(_packet);
                break;
            case Packet_NewGameCancelFail _packet:
                HandlePacket_NewGameCancelFail(_packet);
                break;
            case Packet_NewGameCanceled _packet:
                HandlePacket_NewGameCanceled(_packet);
                break;
            case Packet_LobbyDisconnectOther _packet:
                HandlePacket_LobbyDisconnectOther(_packet);
                break;
            case Packet_LobbyTimeoutWarning _packet:
                HandlePacket_LobbyTimeoutWarning(_packet);
                break;
            case Packet_LobbyTimeout _packet:
                HandlePacket_LobbyTimeout(_packet);
                break;
            case Packet_NewGameStarting _packet:
                HandlePacket_NewGameStarting(_packet);
                break;
            case Packet_GameActionPlaceOk _packet:
                HandlePacket_GameActionPlaceOk(_packet);
                break;
            case Packet_GameActionPlaceFail _packet:
                HandlePacket_GameActionPlaceFail(_packet);
                break;
            case Packet_GameActionPlaceOther _packet:
                HandlePacket_GameActionPlaceOther(_packet);
                break;
            case Packet_GameActionRefillOk _packet:
                HandlePacket_GameActionRefillOk(_packet);
                break;
            case Packet_GameActionRefillFail _packet:
                HandlePacket_GameActionRefillFail(_packet);
                break;
            case Packet_GameActionRefillOther _packet:
                HandlePacket_GameActionRefillOther(_packet);
                break;
            case Packet_GameFinished _packet:
                HandlePacket_GameFinished(_packet);
                break;
            case Packet_ServerClosing _packet:
                HandlePacket_ServerClosing(_packet);
                break;
            default:
                GD.Print($"Client did not expect to get packet of type {packet.GetType().Name}");
                Desync();
                break;
        }
    }

    private void HandlePacket_Dummy(Packet_Dummy packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Got dummy packet");
    }

    private void HandlePacket_InvalidPacket(Packet_InvalidPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got invalid packet: {packet.GivenPacketType}");
        DisplayError("Bad packet from server");
        Desync();
    }

    private void HandlePacket_InvalidPacketInform(Packet_InvalidPacketInform packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server informed about invalid packet: {packet.GivenPacketType}");
        DisplayError("Something went wrong while communicating with the server");
        Desync();
    }

    private void HandlePacket_CreateLobbyOk(Packet_CreateLobbyOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server created lobby: {packet.LobbyId}");
        if(_lobby is not null)
        {
            GD.Print("But I am already in a lobby??");
            Desync();
            return;
        }
        if(_lobbyConnectionPacket is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }

        _lobby = packet.LobbyId;
        _isPlayer1 = true;
        _lobbyConnectionPacket = null;
        _otherPlayer = null;
        EmitSignal(SignalName.LobbyEntered, packet.LobbyId, ClientName, _otherPlayer!, (bool)_isPlayer1);
    }

    private void HandlePacket_CreateLobbyFail(Packet_CreateLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Creating lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionPacket is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }
        _lobbyConnectionPacket = null;
        DisplayError($"Creating lobby failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
    }

    private void HandlePacket_ConnectLobbyOk(Packet_ConnectLobbyOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.OtherPlayerName);
        GD.Print($"Connected to lobby! Other player: {packet.OtherPlayerName}");
        if(_lobbyConnectionPacket is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }
        _lobby = _lobbyConnectionPacket.LobbyId;
        //empty string means only we are in the lobby
        _otherPlayer = (packet.OtherPlayerName == "")?null:packet.OtherPlayerName;
        _isPlayer1 = _otherPlayer is null;

        string? player1Name = (bool)_isPlayer1 ? ClientName : _otherPlayer;
        string? player2Name = (bool)_isPlayer1 ? _otherPlayer : ClientName;

        EmitSignal(SignalName.LobbyEntered, _lobbyConnectionPacket.LobbyId, player1Name!, player2Name!, (bool)_isPlayer1);
        _lobbyConnectionPacket = null;
    }

    private void HandlePacket_ConnectLobbyFail(Packet_ConnectLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Connecting to lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionPacket is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }

        DisplayError($"Connecting to lobby failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");

        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionPacket = null;
        _otherPlayer = null;
    }

    private void HandlePacket_LobbyNewPlayer(Packet_LobbyNewPlayer packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.OtherPlayerName);
        GD.Print($"New player joined lobby: {packet.OtherPlayerName}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        _otherPlayer = packet.OtherPlayerName;

        EmitSignal(SignalName.LobbyStateUpdated, ClientName, _otherPlayer, true);
    }

    private void HandlePacket_NewGameRequestOk(Packet_NewGameRequestOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Sending new game request was succesful");
        if(_gameRequestPacket is null)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _iHaveRequest = true;
        EmitSignal(SignalName.NewGameRequestSent);
        _gameRequestPacket = null;
    }

    private void HandlePacket_NewGameRequestFail(Packet_NewGameRequestFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Sending new game request failed with error: {packet.ErrorCode}");
        if(_gameRequestPacket is null)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _iHaveRequest = false;
        //due to timing we might send the game request before we receive the other player's
        //if that happens we move on and don't display an error
        if(!_otherPlayerHasRequest)
            DisplayError($"Sending game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _gameRequestPacket = null;
    }

    private void HandlePacket_NewGameRequested(Packet_NewGameRequested packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Other player wants to start a game");
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }
        if(_iHaveRequest || _otherPlayerHasRequest)
        {
            GD.Print("But there's already a request??");
            Desync();
            return;
        }
        _otherPlayerHasRequest = true;
        EmitSignal(SignalName.NewGameRequestReceived);
    }

    private void HandlePacket_NewGameAcceptOk(Packet_NewGameAcceptOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Accepting new game request was succesful");
        if(_gameAcceptPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _otherPlayerHasRequest = false;
        _iHaveRequest = false;
        _gameShouldStart = true;
        EmitSignal(SignalName.NewGameAcceptSent);
        _gameAcceptPacket = null;
    }

    private void HandlePacket_NewGameAcceptFail(Packet_NewGameAcceptFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Accepting a new game request failed with error: {packet.ErrorCode}");
        if(_gameAcceptPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Accepting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _gameAcceptPacket = null;
    }

    private void HandlePacket_NewGameAccepted(Packet_NewGameAccepted packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was accepted!");
        if(!_iHaveRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _iHaveRequest = false;
        _gameShouldStart = true;
        EmitSignal(SignalName.NewGameAcceptReceived);
    }

    private void HandlePacket_NewGameRejectOk(Packet_NewGameRejectOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Rejecting new game request was succesful");
        if(_gameRejectPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _otherPlayerHasRequest = false;
        EmitSignal(SignalName.NewGameRejectSent);
        _gameRejectPacket = null;
    }

    private void HandlePacket_NewGameRejectFail(Packet_NewGameRejectFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Rejecting a new game request failed with error: {packet.ErrorCode}");
        if(_gameRejectPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Rejecting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _gameRejectPacket = null;
    }

    private void HandlePacket_NewGameRejected(Packet_NewGameRejected packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was rejected :(");
        if(!_iHaveRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _iHaveRequest = false;
        EmitSignal(SignalName.NewGameRejectReceived);
    }

    private void HandlePacket_NewGameCancelOk(Packet_NewGameCancelOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Canceling a new game request was succesful");
        if(_gameCancelPacket is null)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        _iHaveRequest = false;
        EmitSignal(SignalName.NewGameCancelSent);
        _gameCancelPacket = null;
    }

    private void HandlePacket_NewGameCancelFail(Packet_NewGameCancelFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Canceling a new game request failed with error: {packet.ErrorCode}");
        if(_gameCancelPacket is null)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        DisplayError($"Canceling game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _gameCancelPacket = null;
    }

    private void HandlePacket_NewGameCanceled(Packet_NewGameCanceled packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was canceled");
        if(!_otherPlayerHasRequest)
        {
            GD.Print("But there's no request??");
            Desync();
            return;
        }
        _otherPlayerHasRequest = false;
        EmitSignal(SignalName.NewGameCancelReceived);
    }

    private void HandlePacket_LobbyDisconnectOther(Packet_LobbyDisconnectOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Other player disconnected: {packet.Reason}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }

        if(_inGame)
        {
            _inGame = false;
            EmitSignal(SignalName.GameEjected);
        }

        if(!(bool)_isPlayer1!) _isPlayer1 = true;

        EmitSignal(SignalName.LobbyStateUpdated, ClientName, "", true);

        _otherPlayer = null;
        _gameRequestPacket = null;
        _gameAcceptPacket = null;
        _gameRejectPacket = null;
        _iHaveRequest = false;
        _otherPlayerHasRequest = false;
        _gameShouldStart = false;
    }
    private void HandlePacket_LobbyTimeoutWarning(Packet_LobbyTimeoutWarning packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Lobby will timeout in {packet.SecondsRemaining}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        EmitSignal(SignalName.LobbyTimeoutWarned, packet.SecondsRemaining);
    }

    private void HandlePacket_LobbyTimeout(Packet_LobbyTimeout packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Lobby timed out");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        EmitSignal(SignalName.LobbyTimedOut);
        _lobby = null;
        _lobbyConnectionPacket = null;
        _otherPlayer = null;
        _gameRequestPacket = null;
        _gameAcceptPacket = null;
        _gameRejectPacket = null;
        _iHaveRequest = false;
        _otherPlayerHasRequest = false;
        _gameShouldStart = false;
        _inGame = false;
        _isPlayer1 = false;
    }

    private void HandlePacket_NewGameStarting(Packet_NewGameStarting packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"New game is starting! My color: {packet.GameTurn}");
        if(!_gameShouldStart)
        {
            GD.Print("But I was not aware of that??");
            Desync();
            return;
        }
        _gameShouldStart = false;
        _inGame = true;
        EmitSignal(SignalName.GameStarted, (int)packet.GameTurn);
    }

    private void HandlePacket_GameActionPlaceOk(Packet_GameActionPlaceOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Placing was succesful");
        if(!_inGame || Game is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        if(_placePacket is null)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        if(!ResourceLoader.Exists(_placePacket.ScenePath))
        {
            GD.Print("Server approved place action but the scene does not exist??");
            Desync();
            return;
        }
        Resource res = ResourceLoader.Load(_placePacket.ScenePath);
        if(res is not PackedScene scene)
        {
            GD.Print("Server approved place action but the resource is not a scene??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionPlaceSent, _placePacket.Column, scene);
        _placePacket = null;
    }

    private void HandlePacket_GameActionPlaceFail(Packet_GameActionPlaceFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Placing failed with error: {packet.ErrorCode}");
        if(_placePacket is null)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        _placePacket = null;
    }

    private void HandlePacket_GameActionPlaceOther(Packet_GameActionPlaceOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.ScenePath);
        GD.Print($"Other player is placing token at {packet.Column}. Token type: {packet.ScenePath}");
        if(!_inGame)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        if(!ResourceLoader.Exists(packet.ScenePath))
        {
            GD.Print("Other player sent game action place with nonexistent scene path??");
            Desync();
            return;
        }
        Resource res = ResourceLoader.Load(packet.ScenePath);
        if(res is not PackedScene scene)
        {
            GD.Print("Other player sent game action place with path that points to a non scene resource??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionPlaceReceived, packet.Column, scene);
    }

    private void HandlePacket_GameActionRefillOk(Packet_GameActionRefillOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Refill was succesful");
        if(_refillPacket is null)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionRefillSent);
        _refillPacket = null;
    }

    private void HandlePacket_GameActionRefillFail(Packet_GameActionRefillFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Refilling failed with error: {packet.ErrorCode}");
        if(_refillPacket is null)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        _refillPacket = null;
    }

    private void HandlePacket_GameActionRefillOther(Packet_GameActionRefillOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Other player is refilling");
        if(!_inGame)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionRefillReceived);
    }

    private void HandlePacket_GameFinished(Packet_GameFinished packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Game finished! Result: {packet.Result}. Player 1 score: {packet.Player1Score}. Player 2 score: {packet.Player2Score}");
        if(!_inGame)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        _inGame = false;
        EmitSignal(SignalName.GameFinished);
    }

    private void HandlePacket_ServerClosing(Packet_ServerClosing packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Server closing!");
        EmitSignal(SignalName.ServerClosed);
    }

    #endregion

    #region Operations

    public void CreateLobby()
    {
        if(_lobby is not null) return;
        SendPacket(new Packet_CreateLobbyRequest(ClientName));
    }
    public void JoinLobby(uint lobby)
    {
        if(_lobby is not null) return;
        _lobbyConnectionPacket = new Packet_ConnectLobbyRequest(lobby, ClientName);
        SendPacket(_lobbyConnectionPacket);
    }
    public void DisconnectFromLobby(DisconnectReasonEnum reason)
    {
        if(_lobby is null) return;
        SendPacket(new Packet_LobbyDisconnect(reason));
        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionPacket = null;
        _otherPlayer = null;
        _gameRequestPacket = null;
        _gameAcceptPacket = null;
        _gameRejectPacket = null;
        _iHaveRequest = false;
        _otherPlayerHasRequest = false;
        _gameShouldStart = false;
        _inGame = false;
    }
    public void DisconnectFromServer(DisconnectReasonEnum reason)
    {
        DisconnectFromLobby(reason);
        CloseConnection();
    }
    public void RequestNewGame()
    {
        if(_lobby is null || _otherPlayer is null || _otherPlayerHasRequest || _iHaveRequest || _gameShouldStart || _inGame) return;

        _gameRequestPacket = new Packet_NewGameRequest();
        SendPacket(_gameRequestPacket);
    }
    public void AcceptNewGame()
    {
        if(_lobby is null || !_otherPlayerHasRequest) return;

        _gameAcceptPacket = new Packet_NewGameAccept();
        SendPacket(_gameAcceptPacket);
    }
    public void RejectNewGame()
    {
        if(_lobby is null || !_otherPlayerHasRequest) return;

        _gameRejectPacket = new Packet_NewGameReject();
        SendPacket(_gameRejectPacket);
    }
    public void CancelNewGame()
    {
        if(_lobby is null || !_iHaveRequest) return;

        _gameCancelPacket = new Packet_NewGameCancel();
        SendPacket(_gameCancelPacket);
    }
    public void PlaceToken(byte column, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        //if(!_inGame) return;

        SendPacket(new Packet_GameActionPlace(column, path));
    }

    public void Refill()
    {
        //if(!_inGame) return;

        SendPacket(new Packet_GameActionRefill());
    }

    public void Desync()
    {
        GD.PushError("Desync detected");
        DisplayError("Something went wrong while communicating with the server");
        DisconnectFromServer(DisconnectReasonEnum.DESYNC);
    }

    #endregion

    public void CloseConnection()
    {
        if(_client.State != WebSocketPeer.State.Open)
            return;
        
        _client.Close();
        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionPacket = null;
        _otherPlayer = null;
        _gameRequestPacket = null;
        _gameAcceptPacket = null;
        _gameRejectPacket = null;
        _iHaveRequest = false;
        _otherPlayerHasRequest = false;
        _gameShouldStart = false;
        _inGame = false;
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        EmitSignal(SignalName.ErrorOccured, error);
    }
}