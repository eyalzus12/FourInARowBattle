using DequeNet;
using Godot;
using System.Diagnostics.CodeAnalysis;

namespace FourInARowBattle;

public abstract class AbstractPacket
{
    public abstract PacketTypeEnum PacketType{get;}

    public abstract byte[] ToByteArray();

    //Attempt to construct a packet from the buffer
    //If there's a full valid packet, true is returned, packet is the resulting packet, and the used data is taken out of buffer
    //If there isn't a full valid packet, false is returned and packet is null
    public static bool TryConstructFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 1) return false;

        PacketTypeEnum type = (PacketTypeEnum)buffer[0];
        //one of my favourite tricks:
        //use curly brackets for each case to create a new scope and avoid name collision
        //this could've been done better if C# had proper abstract static methods
        //but oh well. this is pretty clean.
        switch(type)
        {
            case PacketTypeEnum.DUMMY:
            {
                buffer.PopLeft();
                packet = new Packet_Dummy();
                return true;
            }
            case PacketTypeEnum.CREATE_LOBBY_REQUEST:
            {
                if(buffer.Count < 5) return false;
                byte size = buffer[1];
                if(buffer.Count < 5 + size) return false;
                for(int i = 0; i < 2; ++i) buffer.PopLeft();
                byte[] name = new byte[size]; for(int i = 0; i < size; ++i) name[i] = buffer.PopLeft();
                packet = new Packet_CreateLobbyRequest(){PlayerName = name.GetStringFromUtf8()};
                return true;
            }
            case PacketTypeEnum.CREATE_LOBBY_OK:
            {
                if(buffer.Count < 5) return false;
                uint lobbyId = Utils.LoadBigEndianU32(new[]{buffer[1], buffer[2], buffer[3], buffer[4]}, 0);
                for(int i = 0; i < 5; ++i) buffer.PopLeft();
                packet = new Packet_CreateLobbyOk(){LobbyId = lobbyId};
                return true;
            }
            case PacketTypeEnum.CREATE_LOBBY_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_CreateLobbyFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.CONNECT_LOBBY_REQUEST:
            {
                if(buffer.Count < 6) return false;
                byte size = buffer[5];
                if(buffer.Count < 6 + size) return false;
                uint lobbyId = Utils.LoadBigEndianU32(new[]{buffer[1], buffer[2], buffer[3], buffer[4]}, 0);
                for(int i = 0; i < 6; ++i) buffer.PopLeft();
                byte[] name = new byte[size]; for(int i = 0; i < size; ++i) name[i] = buffer.PopLeft();
                packet = new Packet_ConnectLobbyRequest(){LobbyId = lobbyId, PlayerName = name.GetStringFromUtf8()};
                return true;
            }
            case PacketTypeEnum.CONNECT_LOBBY_OK:
            {
                if(buffer.Count < 2) return false;
                byte size = buffer[1];
                if(buffer.Count < 2 + size) return false;
                for(int i = 0; i < 2; ++i) buffer.PopLeft();
                byte[] name = new byte[size]; for(int i = 0; i < size; ++i) name[i] = buffer.PopLeft();
                packet = new Packet_ConnectLobbyOk(){OtherPlayerName = name.GetStringFromUtf8()};
                return true;
            }
            case PacketTypeEnum.CONNECT_LOBBY_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_ConnectLobbyFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.LOBBY_NEW_PLAYER:
            {
                if(buffer.Count < 2) return false;
                byte size = buffer[1];
                if(buffer.Count < 2 + size) return false;
                for(int i = 0; i < 2; ++i) buffer.PopLeft();
                byte[] name = new byte[size]; for(int i = 0; i < size; ++i) name[i] = buffer.PopLeft();
                packet = new Packet_LobbyNewPlayer(){OtherPlayerName = name.GetStringFromUtf8()};
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REQUEST:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameRequest();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REQUEST_OK:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameRequestOk();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REQUEST_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_NewGameRequestFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REQUESTED:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameRequested();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_ACCEPT:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameAccept();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_ACCEPT_OK:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameAcceptOk();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_ACCEPT_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_NewGameAcceptFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.NEW_GAME_ACCEPTED:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameAccepted();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REJECT:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameReject();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REJECT_OK:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameRejectOk();
                return true;
            }   
            case PacketTypeEnum.NEW_GAME_REJECT_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_NewGameRejectFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.NEW_GAME_REJECTED:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameRejected();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_CANCEL:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameCancel();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_CANCEL_OK:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameCancelOk();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_CANCEL_FAIL:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                ErrorCodeEnum error = (ErrorCodeEnum)buffer.PopLeft();
                packet = new Packet_NewGameCancelFail(){ErrorCode = error};
                return true;
            }
            case PacketTypeEnum.NEW_GAME_CANCELED:
            {
                buffer.PopLeft();
                packet = new Packet_NewGameCanceled();
                return true;
            }
            case PacketTypeEnum.LOBBY_DISCONNECT:
            {
                buffer.PopLeft();
                packet = new Packet_LobbyDisconnect();
                return true;
            }
            case PacketTypeEnum.LOBBY_DISCONNECT_OTHER:
            {
                buffer.PopLeft();
                packet = new Packet_LobbyDisconnectOther();
                return true;
            }
            case PacketTypeEnum.LOBBY_TIMEOUT_WARNING:
            {
                if(buffer.Count < 5) return false;
                uint secondsRemaining = Utils.LoadBigEndianU32(new[]{buffer[1], buffer[2], buffer[3], buffer[4]}, 0);
                for(int i = 0; i < 5; ++i) buffer.PopLeft();
                packet = new Packet_LobbyTimeoutWarning(){SecondsRemaining = secondsRemaining};
                return true;
            }
            case PacketTypeEnum.LOBBY_TIMEOUT:
            {
                buffer.PopLeft();
                packet = new Packet_LobbyTimeout();
                return true;
            }
            case PacketTypeEnum.NEW_GAME_STARTING:
            {
                if(buffer.Count < 2) return false;
                buffer.PopLeft();
                GameTurnEnum turn = (GameTurnEnum)buffer.PopLeft();
                packet = new Packet_NewGameStarting(){GameTurn = turn};
                return true;
            }
            case PacketTypeEnum.GAME_ACTION_PLACE:
            {
                if(buffer.Count < 6) return false;
                uint size = Utils.LoadBigEndianU32(new[]{buffer[2], buffer[3], buffer[4], buffer[5]}, 0);
                if(buffer.Count < 6 + size) return false;
                buffer.PopLeft();
                byte column = buffer.PopLeft();
                for(int i = 0; i < 4; ++i) buffer.PopLeft();
                byte[] path = new byte[size];
                for(int i = 0; i < size; ++i) path[i] = buffer.PopLeft();
                packet = new Packet_GameActionPlace(){Column = column, ScenePath = path.GetStringFromUtf8()};
                return true;
            }
            case PacketTypeEnum.GAME_ACTION_PLACE_OK:
                break;
            case PacketTypeEnum.GAME_ACTION_PLACE_FAIL:
                break;
            case PacketTypeEnum.GAME_ACTION_PLACE_OTHER:
                break;
            case PacketTypeEnum.GAME_ACTION_REFILL:
                break;
            case PacketTypeEnum.GAME_ACTION_REFILL_OK:
                break;
            case PacketTypeEnum.GAME_ACTION_REFILL_FAIL:
                break;
            case PacketTypeEnum.GAME_ACTION_REFILL_OTHER:
                break;
            case PacketTypeEnum.GAME_FINISHED:
                break;
            default:
                GD.PushError($"Unknown packet type {type}");
                return false;
        }
    }
}
