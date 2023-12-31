using Godot;
using DequeNet;

namespace FourInARowBattle;

public partial class GameServer : Node
{
    [Export]
    public WebSocketServer? Server{get; set;}

    private readonly Deque<byte> _buffer = new();

    private readonly LobbyManager _lobbyManager = new();

    public override void _Ready()
    {
        if(Server is not null)
            Server.PacketReceived += OnWebSocketServerPacketReceived;
    }

    public void OnWebSocketServerPacketReceived(int peerId, byte[] packetBytes)
    {
        foreach(byte b in packetBytes) _buffer.PushRight(b);
        if(_buffer.Count > 0)
        {
            if(AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
            {
                HandlePacket(peerId, packet);
            }
        }
    }

    public void SendPacket(int peerId, AbstractPacket packet)
    {
        Server?.SendPacket(peerId, packet.ToByteArray());
    }

    public void HandlePacket(int peerId, AbstractPacket packet)
    {
        switch(packet)
        {
            case Packet_Dummy:
            {
                GD.Print($"Got dummy packet from {peerId}");
                break;
            }
            case Packet_InvalidPacket _packet:
            {
                GD.Print($"Got invalid packet from {peerId}: {_packet.GivenPacketType}");
                break;
            }
            case Packet_InvalidPacketInform:
            {
                GD.PushError($"I'm server. Why is {peerId} informing me of invalid packet?");
                break;
            }
            case Packet_CreateLobbyRequest _packet:
            {
                GD.Print($"{peerId} wants to create lobby. Player name: {_packet.PlayerName}");
                _lobbyManager.RegisterNewPlayer(peerId, _packet.PlayerName);
                uint? lobby = _lobbyManager.GetPlayerLobby(peerId);
                if(lobby is not null)
                {
                    SendPacket(peerId, new Packet_CreateLobbyFail(){ErrorCode = ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY});
                }
                else
                {
                    uint lobbyId = _lobbyManager.GetAvailableLobbyId();
                    _lobbyManager.CreateNewLobby(lobbyId);
                    _lobbyManager.AddPlayerToLobby(peerId, lobbyId, out string? _);
                    SendPacket(peerId, new Packet_CreateLobbyOk());
                }
                break;
            }
            case Packet_CreateLobbyOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send create lobby ok?");
                break;
            }
            case Packet_CreateLobbyFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send create lobby fail?");
                break;
            }
            case Packet_ConnectLobbyRequest _packet:
            {
                GD.Print($"{peerId} wants to connect to lobby {_packet.LobbyId} with name {_packet.PlayerName}");
                _lobbyManager.RegisterNewPlayer(peerId, _packet.PlayerName);
                LobbyManagerErrorEnum err = _lobbyManager.AddPlayerToLobby(peerId, _packet.LobbyId, out string? other);
                switch(err)
                {
                    case LobbyManagerErrorEnum.NONE:
                        SendPacket(peerId, new Packet_ConnectLobbyOk(){OtherPlayerName = other});
                        break;
                    case LobbyManagerErrorEnum.LOBBY_DOES_NOT_EXIST:
                        SendPacket(peerId, new Packet_ConnectLobbyFail(){ErrorCode = ErrorCodeEnum.CANNOT_JOIN_LOBBY_DOES_NOT_EXIST});
                        break;
                    case LobbyManagerErrorEnum.PLAYER_ALREADY_IN_LOBBY or LobbyManagerErrorEnum.PLAYER_ALREADY_IN_THAT_LOBBY:
                        SendPacket(peerId, new Packet_ConnectLobbyFail(){ErrorCode = ErrorCodeEnum.CANNOT_JOIN_WHILE_IN_LOBBY});
                        break;
                    case LobbyManagerErrorEnum.LOBBY_IS_FULL:
                        SendPacket(peerId, new Packet_ConnectLobbyFail(){ErrorCode = ErrorCodeEnum.CANNOT_JOIN_LOBBY_FULL});
                        break;
                    default:
                        GD.PushError($"Unexpected lobby manager error: {err}");
                        break;
                }
                break;
            }
            case Packet_ConnectLobbyOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send connect lobby ok?");
                break;
            }
            case Packet_ConnectLobbyFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send lobby fail?");
                break;
            }
            case Packet_LobbyNewPlayer:
            {
                GD.PushError($"I'm server. Why did {peerId} send lobby new player?");
                break;
            }
            case Packet_NewGameRequest:
            {
                GD.Print($"{peerId} wants to start new game");
                LobbyManagerErrorEnum err = _lobbyManager.MakeRequest(peerId, out int? other);
                switch(err)
                {
                    case LobbyManagerErrorEnum.NONE:
                        SendPacket(peerId, new Packet_NewGameRequestOk());
                        SendPacket(other??0, new Packet_NewGameRequested());
                        break;
                    case LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST or LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY:
                        SendPacket(peerId, new Packet_NewGameRequestFail(){ErrorCode = ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY});
                        break;
                    case LobbyManagerErrorEnum.MID_GAME:
                        SendPacket(peerId, new Packet_NewGameRequestFail(){ErrorCode = ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME});
                        break;
                    case LobbyManagerErrorEnum.REQUEST_ALREADY_EXISTS:
                        SendPacket(peerId, new Packet_NewGameRequestFail(){ErrorCode = ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID});
                        break;
                    case LobbyManagerErrorEnum.NO_OTHER_PLAYER:
                        SendPacket(peerId, new Packet_NewGameRequestFail(){ErrorCode = ErrorCodeEnum.CANNOT_REQUEST_START_NO_OTHER_PLAYER});
                        break;
                    default:
                        GD.PushError($"Unexpected lobby manager error: {err}");
                        break;
                }
                break;
            }
            case Packet_NewGameRequestOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game request ok?");
                break;
            }
            case Packet_NewGameRequestFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game request fail?");
                break;
            }
            case Packet_NewGameRequested:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game requested?");
                break;
            }
            case Packet_NewGameAccept:
            {
                GD.Print($"{peerId} approves new game request");
                LobbyManagerErrorEnum err = _lobbyManager.ConsumeRequest(peerId, out int? other);
                switch(err)
                {
                    case LobbyManagerErrorEnum.NONE:
                        SendPacket(peerId, new Packet_NewGameAcceptOk());
                        SendPacket(other??0, new Packet_NewGameAccepted());
                        //now start game:
                        
                        break;
                    case LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST or LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY or LobbyManagerErrorEnum.REQUEST_DOES_NOT_EXIST:
                        SendPacket(peerId, new Packet_NewGameAcceptFail(){ErrorCode = ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST});
                        break;
                    default:
                        GD.PushError($"Unexpected lobby manager error: {err}");
                        break;
                }
                break;
            }
            case Packet_NewGameAcceptOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game accept ok?");
                break;
            }
            case Packet_NewGameAcceptFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game accept fail?");
                break;
            }
            case Packet_NewGameAccepted:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game accepted?");
                break;
            }
            case Packet_NewGameReject:
            {
                GD.Print($"{peerId} rejects new game request");
                LobbyManagerErrorEnum err = _lobbyManager.ConsumeRequest(peerId, out int? other);
                switch(err)
                {
                    case LobbyManagerErrorEnum.NONE:
                        SendPacket(peerId, new Packet_NewGameRejectOk());
                        SendPacket(other??0, new Packet_NewGameRejected());
                        break;
                    case LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST or LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY or LobbyManagerErrorEnum.REQUEST_DOES_NOT_EXIST:
                        SendPacket(peerId, new Packet_NewGameRejectFail(){ErrorCode = ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST});
                        break;
                    default:
                        GD.PushError($"Unexpected lobby manager error: {err}");
                        break;
                }
                break;
            }
            case Packet_NewGameRejectOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game reject ok?");
                break;
            }
            case Packet_NewGameRejectFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game reject fail?");
                break;
            }
            case Packet_NewGameRejected:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game rejected?");
                break;
            }
            case Packet_NewGameCancel:
            {
                GD.Print($"{peerId} cancels new game request");
                LobbyManagerErrorEnum err = _lobbyManager.ConsumeRequest(peerId, out int? other);
                switch(err)
                {
                    case LobbyManagerErrorEnum.NONE:
                        SendPacket(peerId, new Packet_NewGameCancelOk());
                        SendPacket(other??0, new Packet_NewGameCanceled());
                        break;
                    case LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST or LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY or LobbyManagerErrorEnum.REQUEST_DOES_NOT_EXIST:
                        SendPacket(peerId, new Packet_NewGameCancelFail(){ErrorCode = ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST});
                        break;
                    default:
                        GD.PushError($"Unexpected lobby manager error: {err}");
                        break;
                }
                break;
            }
            case Packet_NewGameCancelOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game cancel ok?");
                break;
            }
            case Packet_NewGameCancelFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game cancel fail?");
                break;
            }
            case Packet_NewGameCanceled:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game canceled?");
                break;
            }
            case Packet_LobbyDisconnect _packet:
            {
                GD.Print($"{peerId} is disconnecting. Reason: {_packet.Reason}");
                _lobbyManager.GetPlayerOutOfLobby(peerId, out int? other);
                if(other is not null)
                    SendPacket(other??0, new Packet_LobbyDisconnectOther(){Reason = _packet.Reason});
                break;
            }
            case Packet_LobbyDisconnectOther:
            {
                GD.PushError($"I'm server. Why did {peerId} send lobby disconnect other?");
                break;
            }
            case Packet_LobbyTimeoutWarning:
            {
                GD.PushError($"I'm server. Why did {peerId} send lobby timeout warning?");
                break;
            }
            case Packet_LobbyTimeout:
            {
                GD.PushError($"I'm server. Why did {peerId} send lobby timeout?");
                break;
            }
            case Packet_NewGameStarting:
            {
                GD.PushError($"I'm server. Why did {peerId} send new game starting?");
                break;
            }
            case Packet_GameActionPlace _packet:
            {
                GD.Print($"{peerId} is placing {_packet.ScenePath} at {_packet.Column}");
                break;
            }
            case Packet_GameActionPlaceOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action place ok?");
                break;
            }
            case Packet_GameActionPlaceFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action place fail?");
                break;
            }
            case Packet_GameActionPlaceOther:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action place other?");
                break;
            }
            case Packet_GameActionRefill:
            {
                GD.Print($"{peerId} is refilling");
                break;
            }
            case Packet_GameActionRefillOk:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action refill ok?");
                break;
            }
            case Packet_GameActionRefillFail:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action refill fail?");
                break;
            }
            case Packet_GameActionRefillOther:
            {
                GD.PushError($"I'm server. Why did {peerId} send game action refill other?");
                break;
            }
            case Packet_GameFinished:
            {
                GD.PushError($"I'm server. Why did {peerId} send game finished?");
                break;
            }
            default:
            {
                GD.PushError($"Unknown packet type {packet.GetType().Name}");
                break;
            }
        }
    }
}