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
        if(Client is null) { GD.PushError($"No {nameof(Client)} set"); return;}
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
        Client.ConnectToUrl("127.0.0.1");
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            Client?.Close();
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
        if(Client is null) return;

        if(Client.State != WebSocketPeer.State.Open)
        {
            DisplayError("Connection to server is not yet established. Please wait.");
            return;
        }

        Error err = Client.SendPacket(packet.ToByteArray());
        DisplayError($"Error {err} while trying to communicate with server");
    }

    public void HandlePacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        switch(packet)
        {
            case Packet_Dummy:
            {
                GD.Print("Got dummy packet");
                break;
            }
            case Packet_InvalidPacket _packet:
            {
                GD.Print($"Got invalid packet: {_packet.GivenPacketType}");
                DisplayError("Bad packet from server");
                Desync();
                break;
            }
            case Packet_InvalidPacketInform _packet:
            {
                GD.Print($"Server informed about invalid packet: {_packet.GivenPacketType}");
                DisplayError("Something went wrong while communicating with the server");
                Desync();
                break;
            }
            case Packet_CreateLobbyOk _packet:
            {
                GD.Print($"Server created lobby: {_packet.LobbyId}");
                if(_lobby is not null)
                {
                    GD.Print("But I am already in a lobby??");
                    Desync();
                    break;
                }
                if(_lobbyConnectionRequest is not null)
                {
                    GD.Print("But I didn't request that??");
                    Desync();
                    break;
                }

                _lobby = _packet.LobbyId;
                _isPlayer1 = true;
                _lobbyConnectionRequest = null;
                _otherPlayer = null;
                EmitSignal(SignalName.LobbyEntered, _packet.LobbyId, ClientName!, _otherPlayer!, (bool)_isPlayer1);
                break;
            }
            case Packet_CreateLobbyFail _packet:
            {
                GD.Print($"Creating lobby failed with error: {_packet.ErrorCode}");
                if(_lobbyConnectionRequest is not null)
                {
                    GD.Print("But I didn't request that??");
                    Desync();
                    break;
                }
                _lobbyConnectionRequest = null;
                DisplayError($"Creating lobby failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_ConnectLobbyOk _packet:
            {
                ArgumentNullException.ThrowIfNull(_packet.OtherPlayerName);
                GD.Print($"Connected to lobby! Other player: {_packet.OtherPlayerName}");
                if(_lobbyConnectionRequest is null)
                {
                    GD.Print("But I didn't ask to connect??");
                    Desync();
                    break;
                }
                _lobby = _lobbyConnectionRequest;
                //empty string means only us are in the lobby
                _otherPlayer = (_packet.OtherPlayerName == "")?null:_packet.OtherPlayerName;
                _isPlayer1 = _otherPlayer is null;

                EmitSignal(SignalName.LobbyEntered, (uint)_lobbyConnectionRequest, _otherPlayer ?? ClientName!, _otherPlayer!, _otherPlayer is null);
                _lobbyConnectionRequest = null;
                break;
            }
            case Packet_ConnectLobbyFail _packet:
            {
                GD.Print($"Connecting to lobby failed with error: {_packet.ErrorCode}");
                if(_lobbyConnectionRequest is null)
                {
                    GD.Print("But I didn't ask to connect??");
                    Desync();
                    break;
                }

                DisplayError($"Connecting to lobby failed with error: {_packet.ErrorCode}");

                _lobby = null;
                _isPlayer1 = null;
                _lobbyConnectionRequest = null;
                _otherPlayer = null;
                break;
            }
            case Packet_LobbyNewPlayer _packet:
            {
                ArgumentNullException.ThrowIfNull(_packet.OtherPlayerName);
                GD.Print($"New player joined lobby: {_packet.OtherPlayerName}");
                if(_lobby is null)
                {
                    GD.Print("But I am not in a lobby??");
                    Desync();
                    break;
                }
                _otherPlayer = _packet.OtherPlayerName;

                EmitSignal(SignalName.LobbyStateUpdated, ClientName!, _otherPlayer, true);
                break;
            }
            case Packet_NewGameRequestOk:
            {
                GD.Print("Sending new game request was succesful");
                if(!_sentRequest)
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                _sentRequest = false;
                EmitSignal(SignalName.NewGameRequestSent);
                break;
            }
            case Packet_NewGameRequestFail _packet:
            {
                GD.Print($"Sending new game request failed with error: {_packet.ErrorCode}");
                if(!_sentRequest)
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                _sentRequest = false;
                DisplayError($"Sending game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameRequested:
            {
                GD.Print("Other player wants to start a game");
                if(_lobby is null)
                {
                    GD.Print("But I'm not in a lobby??");
                    Desync();
                    break;
                }
                if(_sentRequest || _otherPlayerSentRequest)
                {
                    GD.Print("But there's already a request??");
                    Desync();
                    break;
                }
                _otherPlayerSentRequest = true;
                EmitSignal(SignalName.NewGameRequestReceived);
                break;
            }
            case Packet_NewGameAcceptOk:
            {
                GD.Print("Accepting new game request was succesful");
                if(!_sentAccept)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _sentAccept = false;
                _otherPlayerSentRequest = false;
                _gameShouldStart = true;
                EmitSignal(SignalName.NewGameAcceptSent);
                break;
            }
            case Packet_NewGameAcceptFail _packet:
            {
                GD.Print($"Accepting a new game request failed with error: {_packet.ErrorCode}");
                if(!_sentAccept)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _sentAccept = false;
                DisplayError($"Accepting game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameAccepted:
            {
                GD.Print("New game request was accepted!");
                if(!_sentRequest)
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                _sentRequest = false;
                _gameShouldStart = true;
                EmitSignal(SignalName.NewGameAcceptReceived);
                break;
            }
            case Packet_NewGameRejectOk:
            {
                GD.PushError("Rejecting new game request was succesful");
                if(!_sentReject)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _sentReject = false;
                _otherPlayerSentRequest = false;
                EmitSignal(SignalName.NewGameRejectSent);
                break;
            }
            case Packet_NewGameRejectFail _packet:
            {
                GD.Print($"Rejecting a new game request failed with error: {_packet.ErrorCode}");
                if(!_sentReject)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _sentReject = false;
                DisplayError($"Rejecting game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameRejected:
            {
                GD.Print("New game request was rejected :(");
                if(!_sentRequest)
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                _sentRequest = true;
                EmitSignal(SignalName.NewGameRejectReceived);
                break;
            }
            case Packet_NewGameCancelOk:
            {
                GD.Print("Canceling a new game request was succesful");
                if(!_sentCancel)
                {
                    GD.Print("But I didn't cancel??");
                    Desync();
                    break;
                }
                _sentCancel = false;
                _sentRequest = false;
                EmitSignal(SignalName.NewGameCancelSent);
                break;
            }
            case Packet_NewGameCancelFail _packet:
            {
                GD.Print($"Canceling a new game request failed with error: {_packet.ErrorCode}");
                if(!_sentCancel)
                {
                    GD.Print("But I didn't cancel??");
                    Desync();
                    break;
                }
                _sentCancel = false;
                DisplayError($"Canceling game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameCanceled:
            {
                GD.Print("New game request was canceled");
                if(!_otherPlayerSentRequest)
                {
                    GD.Print("But there's no request??");
                    Desync();
                    break;
                }
                _otherPlayerSentRequest = false;
                EmitSignal(SignalName.NewGameCancelReceived);
                break;
            }
            case Packet_LobbyDisconnectOther _packet:
            {
                GD.Print($"Other player disconnected: {_packet.Reason}");
                if(_lobby is null)
                {
                    GD.Print("But I am not in a lobby??");
                    Desync();
                    break;
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
                break;
            }
            case Packet_LobbyTimeoutWarning _packet:
            {
                GD.Print($"Lobby will timeout in {_packet.SecondsRemaining}");
                if(_lobby is null)
                {
                    GD.Print("But I am not in a lobby??");
                    Desync();
                    break;
                }
                EmitSignal(SignalName.LobbyTimeoutWarned, _packet.SecondsRemaining);
                break;
            }
            case Packet_LobbyTimeout:
            {
                GD.Print("Lobby timed out");
                if(_lobby is null)
                {
                    GD.Print("But I am not in a lobby??");
                    Desync();
                    break;
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
                break;
            }
            case Packet_NewGameStarting _packet:
            {
                GD.Print($"New game is starting! My color: {_packet.GameTurn}");
                if(!_gameShouldStart)
                {
                    GD.Print("But I was not aware of that??");
                    Desync();
                    break;
                }
                _gameShouldStart = false;
                _inGame = true;
                EmitSignal(SignalName.GameStarted, (int)_packet.GameTurn);
                break;
            }
            case Packet_GameActionPlaceOk:
            {
                GD.Print("Placing was succesful");
                if(!_sentPlace)
                {
                    GD.Print("But I didn't send a place request??");
                    Desync();
                    break;
                }
                _sentPlace = false;
                //do place:

                break;
            }
            case Packet_GameActionPlaceFail _packet:
            {
                GD.Print($"Placing failed with error: {_packet.ErrorCode}");
                if(!_sentPlace)
                {
                    GD.Print("But I didn't send a place request??");
                    Desync();
                    break;
                }
                _sentPlace = false;
                //desync if that's illogical:

                break;
            }
            case Packet_GameActionPlaceOther _packet:
            {
                ArgumentNullException.ThrowIfNull(_packet.ScenePath);
                GD.Print($"Other player is placing token at {_packet.Column}. Token type: {_packet.ScenePath}");
                if(!_inGame)
                {
                    GD.Print("But I'm not in a game??");
                    Desync();
                    break;
                }
                //handle place:
                
                break;
            }
            case Packet_GameActionRefillOk:
            {
                GD.Print("Refill was succesful");
                if(!_sentRefill)
                {
                    GD.Print("But I didn't send a refill??");
                    Desync();
                    break;
                }
                //do refill:

                break;
            }
            case Packet_GameActionRefillFail _packet:
            {
                GD.Print($"Refilling failed with error: {_packet.ErrorCode}");
                if(!_sentRefill)
                {
                    GD.Print("But I didn't send a refill??");
                    Desync();
                    break;
                }
                //desync if illogical:

                break;
            }
            case Packet_GameActionRefillOther:
            {
                GD.Print("Other player is refilling");
                if(!_inGame)
                {
                    GD.Print("But I'm not in a game??");
                    Desync();
                    break;
                }
                //handle refill:

                break;
            }
            case Packet_GameFinished _packet:
            {
                GD.Print($"Game finished! Result: {_packet.Result}. Player 1 score: {_packet.Player1Score}. Player 2 score: {_packet.Player2Score}");
                if(!_inGame)
                {
                    GD.Print("But I'm not in a game??");
                    Desync();
                    break;
                }
                _inGame = false;
                EmitSignal(SignalName.GameFinished);

                break;
            }
            default:
            {
                GD.Print($"Client did not expect to get packet of type {packet.GetType().Name}");
                Desync();
                break;
            }
        }
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

    public void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        EmitSignal(SignalName.ErrorOccured, error);
    }
}