using Godot;
using DequeNet;
using System;

namespace FourInARowBattle;

public partial class GameClient : Node
{
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
    public delegate void GameFinishedEventHandler();

    #endregion


    [Export]
    public WebSocketClient Client{get; set;} = null!;

    private readonly Deque<byte> _buffer = new();

    public string? ClientName{get; set;}

    #region State Variables

    //curent lobby
    private uint? _lobby = null;
    private bool? _isPlayer1 = null;
    //lobby connect request
    private uint? _lobbyConnectionRequest = null;
    //name of other player
    private string? _otherPlayer = null;

    private bool _sentRequest = false;
    private bool _sentReject = false;
    private bool _sentAccept = false;
    private bool _sentCancel = false;

    private bool _otherPlayerSentRequest = false;

    private bool _gameShouldStart = false;
    private bool _inGame = false;

    private bool _sentPlace = false;
    private bool _sentRefill = false;

    #endregion

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Client);
    }

    private void ConnectSignals()
    {
        Client.PacketReceived += OnWebSocketClientPacketReceived;
        Client.ConnectedToServer += OnWebSocketClientConnected;
        Client.ConnectionClosed += OnWebSocketClientConnectionClosed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        Error err = Client.ConnectToUrl("127.0.0.1:1234");
        if(err != Error.Ok)
        {
            DisplayError($"Error while trying to connect: {err}");
        }
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            Client.Close();
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

        GD.Print("sending packet");

        if(Client.State != WebSocketPeer.State.Open)
        {
            DisplayError("Connection to server is not yet established. Please wait.");
            return;
        }

        Error err = Client.SendPacket(packet.ToByteArray());
        if(err != Error.Ok)
            DisplayError($"Error {err} while trying to communicate with server");
    }

    public void HandlePacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        //giant switch statement to handle all packets
        //can't use polymorphism since that would require to expose the GameClient privates to the packets.
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
        GD.Print($"Got invalid _packet: {packet.GivenPacketType}");
        DisplayError("Bad packet from server");
        Desync();
    }

    private void HandlePacket_InvalidPacketInform(Packet_InvalidPacketInform packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server informed about invalid _packet: {packet.GivenPacketType}");
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
        if(_lobbyConnectionRequest is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }

        _lobby = packet.LobbyId;
        _isPlayer1 = true;
        _lobbyConnectionRequest = null;
        _otherPlayer = null;
        EmitSignal(SignalName.LobbyEntered, packet.LobbyId, ClientName!, _otherPlayer!, (bool)_isPlayer1);
    }

    private void HandlePacket_CreateLobbyFail(Packet_CreateLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Creating lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionRequest is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }
        _lobbyConnectionRequest = null;
        DisplayError($"Creating lobby failed with error: {packet.ErrorCode}");
    }

    private void HandlePacket_ConnectLobbyOk(Packet_ConnectLobbyOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.OtherPlayerName);
        GD.Print($"Connected to lobby! Other player: {packet.OtherPlayerName}");
        if(_lobbyConnectionRequest is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }
        _lobby = _lobbyConnectionRequest;
        //empty string means only us are in the lobby
        _otherPlayer = (packet.OtherPlayerName == "")?null:packet.OtherPlayerName;
        _isPlayer1 = _otherPlayer is null;

        EmitSignal(SignalName.LobbyEntered, (uint)_lobbyConnectionRequest, _otherPlayer ?? ClientName!, _otherPlayer!, _otherPlayer is null);
        _lobbyConnectionRequest = null;
    }

    private void HandlePacket_ConnectLobbyFail(Packet_ConnectLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Connecting to lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionRequest is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }

        DisplayError($"Connecting to lobby failed with error: {packet.ErrorCode}");

        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionRequest = null;
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

        EmitSignal(SignalName.LobbyStateUpdated, ClientName!, _otherPlayer, true);
    }

    private void HandlePacket_NewGameRequestOk(Packet_NewGameRequestOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Sending new game request was succesful");
        if(!_sentRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _sentRequest = false;
        EmitSignal(SignalName.NewGameRequestSent);
    }

    private void HandlePacket_NewGameRequestFail(Packet_NewGameRequestFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Sending new game request failed with error: {packet.ErrorCode}");
        if(!_sentRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _sentRequest = false;
        DisplayError($"Sending game request failed with error: {packet.ErrorCode}");
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
        if(_sentRequest || _otherPlayerSentRequest)
        {
            GD.Print("But there's already a request??");
            Desync();
            return;
        }
        _otherPlayerSentRequest = true;
        EmitSignal(SignalName.NewGameRequestReceived);
    }

    private void HandlePacket_NewGameAcceptOk(Packet_NewGameAcceptOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Accepting new game request was succesful");
        if(!_sentAccept)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _sentAccept = false;
        _otherPlayerSentRequest = false;
        _gameShouldStart = true;
        EmitSignal(SignalName.NewGameAcceptSent);
    }

    private void HandlePacket_NewGameAcceptFail(Packet_NewGameAcceptFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Accepting a new game request failed with error: {packet.ErrorCode}");
        if(!_sentAccept)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _sentAccept = false;
        DisplayError($"Accepting game request failed with error: {packet.ErrorCode}");
    }

    private void HandlePacket_NewGameAccepted(Packet_NewGameAccepted packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was accepted!");
        if(!_sentRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _sentRequest = false;
        _gameShouldStart = true;
        EmitSignal(SignalName.NewGameAcceptReceived);
    }

    private void HandlePacket_NewGameRejectOk(Packet_NewGameRejectOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.PushError("Rejecting new game request was succesful");
        if(!_sentReject)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _sentReject = false;
        _otherPlayerSentRequest = false;
        EmitSignal(SignalName.NewGameRejectSent);
    }

    private void HandlePacket_NewGameRejectFail(Packet_NewGameRejectFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Rejecting a new game request failed with error: {packet.ErrorCode}");
        if(!_sentReject)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        _sentReject = false;
        DisplayError($"Rejecting game request failed with error: {packet.ErrorCode}");
    }

    private void HandlePacket_NewGameRejected(Packet_NewGameRejected packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was rejected :(");
        if(!_sentRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        _sentRequest = true;
        EmitSignal(SignalName.NewGameRejectReceived);
    }

    private void HandlePacket_NewGameCancelOk(Packet_NewGameCancelOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Canceling a new game request was succesful");
        if(!_sentCancel)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        _sentCancel = false;
        _sentRequest = false;
        EmitSignal(SignalName.NewGameCancelSent);
    }

    private void HandlePacket_NewGameCancelFail(Packet_NewGameCancelFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Canceling a new game request failed with error: {packet.ErrorCode}");
        if(!_sentCancel)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        _sentCancel = false;
        DisplayError($"Canceling game request failed with error: {packet.ErrorCode}");
    }

    private void HandlePacket_NewGameCanceled(Packet_NewGameCanceled packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("New game request was canceled");
        if(!_otherPlayerSentRequest)
        {
            GD.Print("But there's no request??");
            Desync();
            return;
        }
        _otherPlayerSentRequest = false;
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

        EmitSignal(SignalName.LobbyStateUpdated, ClientName!, "", true);

        _otherPlayer = null;
        _sentRequest = false;
        _sentAccept = false;
        _sentReject = false;
        _otherPlayerSentRequest = false;
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
        _lobbyConnectionRequest = null;
        _otherPlayer = null;
        _sentRequest = false;
        _sentAccept = false;
        _sentReject = false;
        _otherPlayerSentRequest = false;
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
        if(!_sentPlace)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        _sentPlace = false;
        //do place:
    }

    private void HandlePacket_GameActionPlaceFail(Packet_GameActionPlaceFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Placing failed with error: {packet.ErrorCode}");
        if(!_sentPlace)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        _sentPlace = false;
        //desync if that's illogical:
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
        //handle place:
    }

    private void HandlePacket_GameActionRefillOk(Packet_GameActionRefillOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Refill was succesful");
        if(!_sentRefill)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        //do refill:
    }

    private void HandlePacket_GameActionRefillFail(Packet_GameActionRefillFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Refilling failed with error: {packet.ErrorCode}");
        if(!_sentRefill)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        //desync if illogical:
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
        //handle refill:
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
        SendPacket(new Packet_CreateLobbyRequest(ClientName!));
    }
    public void JoinLobby(uint lobby)
    {
        SendPacket(new Packet_ConnectLobbyRequest(lobby, ClientName!));
        _lobbyConnectionRequest = lobby;
    }
    public void DisconnectFromLobby(DisconnectReasonEnum reason)
    {
        SendPacket(new Packet_LobbyDisconnect(reason));
        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionRequest = null;
        _otherPlayer = null;
        _sentRequest = false;
        _sentAccept = false;
        _sentReject = false;
        _otherPlayerSentRequest = false;
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
        if(_lobby is null) return;
        _sentRequest = true;
        SendPacket(new Packet_NewGameRequest());
    }
    public void AcceptNewGame()
    {
        if(_lobby is not null && _otherPlayerSentRequest)
        {
            _sentAccept = true;
            SendPacket(new Packet_NewGameAccept());
        }
    }
    public void RejectNewGame()
    {
        if(_lobby is not null && _otherPlayerSentRequest)
        {
            _sentReject = true;
            SendPacket(new Packet_NewGameReject());
        }
    }
    public void CancelNewGame()
    {
        if(_lobby is not null && _sentRequest)
        {
            _sentCancel = true;
            SendPacket(new Packet_NewGameCancel());
        }
    }
    public void PlaceToken(byte column, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        SendPacket(new Packet_GameActionPlace(column, path));
    }

    public void Refill() => SendPacket(new Packet_GameActionRefill());

    public void Desync()
    {
        GD.PushError("Desync detected");
        DisplayError("Something went wrong while communicating with the server");
        DisconnectFromServer(DisconnectReasonEnum.DESYNC);
    }

    #endregion

    public void CloseConnection()
    {
        Client?.Close();
        _lobby = null;
        _isPlayer1 = null;
        _lobbyConnectionRequest = null;
        _otherPlayer = null;
        _sentRequest = false;
        _sentAccept = false;
        _sentReject = false;
        _otherPlayerSentRequest = false;
        _gameShouldStart = false;
        _inGame = false;
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        EmitSignal(SignalName.ErrorOccured, error);
    }
}