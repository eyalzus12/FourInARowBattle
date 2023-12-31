using Godot;
using DequeNet;

namespace FourInARowBattle;

public partial class GameClient : Node
{
    [Export]
    public WebSocketClient? Client{get; set;}

    private readonly Deque<byte> _buffer = new();

    public string? ClientName{get; set;}

    //curent lobby
    private uint? _lobby = null;
    //lobby connect request
    private uint? _lobbyConnectionRequest = null;
    //name of other player
    private string? _otherPlayer = null;
    //whether there's an active request
    private bool _hasRequest = false;
    //whether that request is my request
    private bool _myRequest = false;
    //whether i gave an answer to that request
    private bool _answeredRequest = false;

    public override void _Ready()
    {
        if(Client is not null)
        {
            Client.PacketReceived += OnWebSocketClientPacketReceived;
        }
        ClientName ??= "Guest";
    }

    public void OnWebSocketClientPacketReceived(byte[] packetBytes)
    {
        foreach(byte b in packetBytes) _buffer.PushRight(b);

        while(_buffer.Count > 0 && AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(packet);
        }
    }
    public void SendPacket(AbstractPacket packet)
    {
        Client?.SendPacket(packet.ToByteArray());
    }

    public void HandlePacket(AbstractPacket packet)
    {
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
                break;
            }
            case Packet_InvalidPacketInform _packet:
            {
                GD.Print($"Server informed about invalid packet: {_packet.GivenPacketType}");
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
                _lobby = _packet.LobbyId;
                _lobbyConnectionRequest = null;
                _otherPlayer = null;
                break;
            }
            case Packet_CreateLobbyFail _packet:
            {
                GD.Print($"Creating lobby failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_ConnectLobbyOk _packet:
            {
                GD.Print($"Connected to lobby! Other player: {_packet.OtherPlayerName}");
                if(_lobbyConnectionRequest is null)
                {
                    GD.Print("But I didn't ask to connect??");
                    Desync();
                    break;
                }
                _lobby = _lobbyConnectionRequest;
                _lobbyConnectionRequest = null;
                //empty string means only us are in the lobby
                _otherPlayer = (_packet.OtherPlayerName == "")?null:_packet.OtherPlayerName;
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
                _lobby = null;
                _lobbyConnectionRequest = null;
                _otherPlayer = null;
                break;
            }
            case Packet_LobbyNewPlayer _packet:
            {
                GD.Print($"New player joined lobby: {_packet.OtherPlayerName}");
                if(_lobby is null)
                {
                    GD.Print("But I am not in a lobby??");
                    Desync();
                    break;
                }
                _otherPlayer = _packet.OtherPlayerName;
                break;
            }
            case Packet_NewGameRequestOk:
            {
                GD.Print("Sending new game request was succesful");
                if(!(_hasRequest && _myRequest))
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                break;
            }
            case Packet_NewGameRequestFail _packet:
            {
                GD.Print($"Sending new game request failed with error: {_packet.ErrorCode}");
                if(!(_hasRequest && _myRequest))
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                _hasRequest = false;
                _myRequest = false;
                _answeredRequest = false;
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
                if(_hasRequest)
                {
                    GD.Print("But there's already a request??");
                    Desync();
                    break;
                }
                _hasRequest = true;
                _myRequest = false;
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameAcceptOk:
            {
                GD.Print("Accepting new game request was succesful");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameAcceptFail _packet:
            {
                GD.Print($"Accepting a new game request failed with error: {_packet.ErrorCode}");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameAccepted:
            {
                GD.Print("New game request was accepted!");
                if(!(_hasRequest && _myRequest))
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                break;
            }
            case Packet_NewGameRejectOk:
            {
                GD.PushError("Rejecting new game request was succesful");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameRejectFail _packet:
            {
                GD.Print($"Rejecting a new game request failed with error: {_packet.ErrorCode}");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't answer??");
                    Desync();
                    break;
                }
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameRejected:
            {
                GD.Print("New game request was rejected :(");
               if(!(_hasRequest && _myRequest))
                {
                    GD.Print("But I don't have a request??");
                    Desync();
                    break;
                }
                break;
            }
            case Packet_NewGameCancelOk:
            {
                GD.Print("Canceling a new game request was succesful");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't cancel??");
                    Desync();
                    break;
                }
                break;
            }
            case Packet_NewGameCancelFail _packet:
            {
                GD.Print($"Canceling a new game request failed with error: {_packet.ErrorCode}");
                if(!_answeredRequest)
                {
                    GD.Print("But I didn't cancel??");
                    Desync();
                    break;
                }
                break;
            }
            case Packet_NewGameCanceled:
            {
                GD.Print("New game request was canceled");
                if(!(_hasRequest && !_myRequest))
                {
                    GD.Print("But there's no request??");
                    Desync();
                    break;
                }
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
                _otherPlayer = null;
                _hasRequest = false;
                _myRequest = false;
                _answeredRequest = false;
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
                _lobby = null;
                _lobbyConnectionRequest = null;
                _otherPlayer = null;
                _hasRequest = false;
                _myRequest = false;
                _answeredRequest = false;
                break;
            }
            case Packet_NewGameStarting _packet:
            {
                GD.Print($"New game is starting! My color: {_packet.GameTurn}");
                break;
            }
            case Packet_GameActionPlaceOk:
            {
                GD.Print("Placing was succesful");
                break;
            }
            case Packet_GameActionPlaceFail _packet:
            {
                GD.Print($"Placing failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_GameActionPlaceOther _packet:
            {
                GD.Print($"Other player is placing token at {_packet.Column}. Token type: {_packet.ScenePath}");
                break;
            }
            case Packet_GameActionRefillOk:
            {
                GD.Print("Refill was succesful");
                break;
            }
            case Packet_GameActionRefillFail _packet:
            {
                GD.Print($"Refilling failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_GameActionRefillOther:
            {
                GD.Print("Other player is refilling");
                break;
            }
            case Packet_GameFinished _packet:
            {
                GD.Print($"Game finished! Result: {_packet.Result}. Player 1 score: {_packet.Player1Score}. Player 2 score: {_packet.Player2Score}");
                break;
            }
            default:
            {
                GD.PushError($"Client did not expect to get packet of type {packet.GetType().Name}");
                break;
            }
        }
    }

    public void CreateLobby()
    {
        SendPacket(new Packet_CreateLobbyRequest(ClientName!));
    }
    public void ConnectToLobby(uint lobby)
    {
        SendPacket(new Packet_ConnectLobbyRequest(lobby, ClientName!));
        _lobbyConnectionRequest = lobby;
    }
    public void DisconnectFromLobby(DisconnectReasonEnum reason)
    {
        SendPacket(new Packet_LobbyDisconnect(reason));
        _lobby = null;
        _lobbyConnectionRequest = null;
        _otherPlayer = null;
        _hasRequest = false;
        _myRequest = false;
        _answeredRequest = false;
    }
    public void DisconnectFromServer(DisconnectReasonEnum reason)
    {
        DisconnectFromLobby(reason);
        Client?.Close();
    }
    public void RequestNewGame()
    {
        if(_lobby is null) return;
        _hasRequest = true;
        _myRequest = true;
        SendPacket(new Packet_NewGameRequest());
    }
    public void AcceptNewGame()
    {
        if(_lobby is not null && _hasRequest && !_myRequest)
        {
            _answeredRequest = true;
            SendPacket(new Packet_NewGameAccept());
        }
    }
    public void RejectNewGame()
    {
        if(_lobby is not null && _hasRequest && !_myRequest)
        {
            _answeredRequest = true;
            SendPacket(new Packet_NewGameReject());
        }
    }
    public void CancelNewGame()
    {
        if(_lobby is not null && _hasRequest && _myRequest)
        {
            _answeredRequest = true;
            SendPacket(new Packet_NewGameCancel());
        }
    }
    public void PlaceToken(byte column, string path) => SendPacket(new Packet_GameActionPlace(column, path));
    public void Refill() => SendPacket(new Packet_GameActionRefill());

    public void Desync()
    {
        GD.PushError("Desync detected");
        DisconnectFromServer(DisconnectReasonEnum.DESYNC);
    }
}