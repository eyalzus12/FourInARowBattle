using DequeNet;
using Godot;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FourInARowBattle;

public abstract partial class AbstractPacket : RefCounted
{
    private delegate bool PacketConstructor(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet);

    private static readonly ReadOnlyDictionary<PacketTypeEnum, PacketConstructor> _packetDict = 
        new Dictionary<PacketTypeEnum, PacketConstructor>()
        {
            {PacketTypeEnum.DUMMY, TryConstructPacket_DummyFrom},
            {PacketTypeEnum.INVALID_PACKET, TryConstructPacket_InvalidPacketFrom},
            {PacketTypeEnum.INVALID_PACKET_INFORM, TryConstructPacket_InvalidPacketInformFrom},
            {PacketTypeEnum.CREATE_LOBBY_REQUEST, TryConstructPacket_CreateLobbyRequestFrom},
            {PacketTypeEnum.CREATE_LOBBY_OK, TryConstructPacket_CreateLobbyOkFrom},
            {PacketTypeEnum.CREATE_LOBBY_FAIL, TryConstructPacket_CreateLobbyFailFrom},
            {PacketTypeEnum.CONNECT_LOBBY_REQUEST, TryConstructPacket_ConnectLobbyRequestFrom},
            {PacketTypeEnum.CONNECT_LOBBY_OK, TryConstructPacket_ConnectLobbyOkFrom},
            {PacketTypeEnum.CONNECT_LOBBY_FAIL, TryConstructPacket_ConnectLobbyFailFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST, TryConstructPacket_NewGameRequestFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST_OK, TryConstructPacket_NewGameRequestOkFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST_FAIL, TryConstructPacket_NewGameRequestFailFrom},
            {PacketTypeEnum.NEW_GAME_REQUESTED, TryConstructPacket_NewGameRequestedFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT, TryConstructPacket_NewGameAcceptFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT_OK, TryConstructPacket_NewGameAcceptOkFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT_FAIL, TryConstructPacket_NewGameAcceptFailFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPTED, TryConstructPacket_NewGameAcceptedFrom},
            {PacketTypeEnum.NEW_GAME_REJECT, TryConstructPacket_NewGameRejectFrom},
            {PacketTypeEnum.NEW_GAME_REJECT_OK, TryConstructPacket_NewGameRejectOkFrom},
            {PacketTypeEnum.NEW_GAME_REJECT_FAIL, TryConstructPacket_NewGameRejectFailFrom},
            {PacketTypeEnum.NEW_GAME_REJECTED, TryConstructPacket_NewGameRejectedFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL, TryConstructPacket_NewGameCancelFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL_OK, TryConstructPacket_NewGameCancelOkFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL_FAIL, TryConstructPacket_NewGameCancelFailFrom},
            {PacketTypeEnum.NEW_GAME_CANCELED, TryConstructPacket_NewGameCanceledFrom},
            {PacketTypeEnum.LOBBY_PLAYER_BUSY_TRUE, TryConstructPacket_LobbyPlayerBusyTrueFrom},
            {PacketTypeEnum.LOBBY_PLAYER_BUSY_FALSE, TryConstructPacket_LobbyPlayerBusyFalseFrom},
            {PacketTypeEnum.LOBBY_NEW_PLAYER, TryConstructPacket_LobbyNewPlayerFrom},
            {PacketTypeEnum.LOBBY_DISCONNECT, TryConstructPacket_LobbyDisconnectFrom},
            {PacketTypeEnum.LOBBY_DISCONNECT_OTHER, TryConstructPacket_LobbyDisconnectOtherFrom},
            {PacketTypeEnum.LOBBY_TIMEOUT_WARNING, TryConstructPacket_LobbyTimeoutWarningFrom},
            {PacketTypeEnum.LOBBY_TIMEOUT, TryConstructPacket_LobbyTimeoutFrom},
            {PacketTypeEnum.SERVER_CLOSING, TryConstructPacket_GameActionPlaceFrom},
            {PacketTypeEnum.NEW_GAME_STARTING, TryConstructPacket_NewGameStartingFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE, TryConstructPacket_GameActionPlaceFailFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_OK, TryConstructPacket_GameActionPlaceOkFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_FAIL, TryConstructPacket_GameActionRefillOkFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_OTHER, TryConstructPacket_GameActionPlaceOtherFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL, TryConstructPacket_GameActionRefillFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_OK, TryConstructPacket_GameActionRefillOtherFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_FAIL, TryConstructPacket_GameActionRefillFailFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_OTHER, TryConstructPacket_GameQuitOkFrom},
            {PacketTypeEnum.GAME_QUIT, TryConstructPacket_GameQuitFrom},
            {PacketTypeEnum.GAME_QUIT_OK, TryConstructPacket_GameQuitOtherFrom},
            {PacketTypeEnum.GAME_QUIT_FAIL, TryConstructPacket_GameQuitFailFrom},
            {PacketTypeEnum.GAME_QUIT_OTHER, TryConstructPacket_ServerClosingFrom},
            {PacketTypeEnum.GAME_FINISHED, TryConstructPacket_GameFinishedFrom},
        }.AsReadOnly();

    public abstract PacketTypeEnum PacketType{get;}
    public abstract byte[] ToByteArray();

    //Attempt to construct a packet from the buffer
    //If there's a full valid packet, true is returned, packet is the resulting packet, and the used data is taken out of buffer
    //If there isn't a full valid packet, false is returned and packet is null
    public static bool TryConstructPacketFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        packet = null;
        if(buffer.Count < 1) return false;
        return _packetDict.GetValueOrDefault((PacketTypeEnum)buffer[0], CreateInvalidPacket)(buffer, out packet);
    }

    #region Packet Constructors

    private static bool TryConstructPacket_DummyFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_Dummy();
        return true;
    }
    
    private static bool TryConstructPacket_InvalidPacketFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        GD.PushWarning("Received packet type INVALID_PACKET, but that packet type is for internal use only. Use INVALID_PACKET_INFORM to respond to an invalid packet");
        buffer.PopLeft();
        packet = new Packet_InvalidPacket(PacketTypeEnum.INVALID_PACKET);
        return true;
    }
    
    private static bool TryConstructPacket_InvalidPacketInformFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_InvalidPacketInform((PacketTypeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_CreateLobbyRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        byte size = buffer[1];
        if(buffer.Count < 2 + size) return false;
        for(int i = 0; i < 2; ++i) buffer.PopLeft();
        byte[] nameBuffer = new byte[size]; for(int i = 0; i < size; ++i) nameBuffer[i] = buffer.PopLeft();
        string name = nameBuffer.GetStringFromUtf8();
        if(name.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Packet has name with invalid length {name.Length}. It will be trimmed.");
            name = new(name.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        packet = new Packet_CreateLobbyRequest(name);
        return true;
    }
    
    private static bool TryConstructPacket_CreateLobbyOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        uint lobbyId = new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_CreateLobbyOk(lobbyId);
        return true;
    }
    
    private static bool TryConstructPacket_CreateLobbyFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_CreateLobbyFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_ConnectLobbyRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        byte size = buffer[5];
        if(buffer.Count < 6 + size) return false;
        uint lobbyId = new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++i) buffer.PopLeft();
        byte[] nameBuffer = new byte[size]; for(int i = 0; i < size; ++i) nameBuffer[i] = buffer.PopLeft();
        string name = nameBuffer.GetStringFromUtf8();
        if(name.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Packet has name with invalid length {name.Length}. It will be trimmed.");
            name = new(name.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        packet = new Packet_ConnectLobbyRequest(lobbyId, name);
        return true;
    }
    
    private static bool TryConstructPacket_ConnectLobbyOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 9) return false;
        int playerCount = (int)new[]{buffer[5], buffer[6], buffer[7], buffer[8]}.ReadBigEndian<uint>();
        int packedBusyCount = (int)Math.Ceiling(playerCount / 8.0);
        //early "enough space" check
        if(buffer.Count < 9 + playerCount + packedBusyCount) return false;
        int index = 9;
        //ensure enough space for names
        for(int i = 0; i < playerCount; ++i)
        {
            if(index >= buffer.Count) return false;
            byte size = buffer[index];
            index += size + 1;
        }
        //not enough for busy bits
        if(index + packedBusyCount > buffer.Count) return false;

        int yourIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        byte[][] nameBuffers = new byte[playerCount][];
        string[] names = new string[playerCount];
        byte[] packedBusy = new byte[packedBusyCount];
        for(int i = 0; i < 9; ++i) buffer.PopLeft();
        //read names
        for(int i = 0; i < playerCount; ++i)
        {
            byte size = buffer.PopLeft();
            nameBuffers[i] = new byte[size];
            for(int j = 0; j < size; ++j)
            {
                nameBuffers[i][j] = buffer.PopLeft();
            }
            names[i] = nameBuffers[i].GetStringFromUtf8();
            if(names[i].Length > Globals.NAME_LENGTH_LIMIT)
            {
                GD.Print($"Packet has name with invalid length {names[i].Length}. It will be trimmed.");
                names[i] = new(names[i].Take(Globals.NAME_LENGTH_LIMIT).ToArray());
            }
        }
        //read busy bits
        for(int i = 0; i < packedBusyCount; ++i) packedBusy[i] = buffer.PopLeft();
        bool[] busyBits = packedBusy.ReadBits(playerCount, 0);
        //convert into player list
        LobbyPlayerData[] players = names
            .Zip(busyBits, (string name, bool busy) => new LobbyPlayerData(name, busy))
            .ToArray();
        packet = new Packet_ConnectLobbyOk(yourIndex, players);
        return true;
    }
    
    private static bool TryConstructPacket_ConnectLobbyFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_ConnectLobbyFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequest(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRequestOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequestOk(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRequestFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int targetIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequestFail(errorCode, targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRequestedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequested(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameAcceptFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAccept(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameAcceptOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAcceptOk(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameAcceptFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int sourceIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAcceptFail(errorCode, sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameAcceptedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAccepted(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRejectFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameReject(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRejectOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRejectOk(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRejectFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int sourceIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRejectFail(errorCode, sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameRejectedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRejected(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameCancelFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameCancel(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameCancelOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameCancelOk(targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameCancelFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int targetIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameCancelFail(errorCode, targetIndex);
        return true;
    }
    
    private static bool TryConstructPacket_NewGameCanceledFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameCanceled(sourceIndex);
        return true;
    }
    
    private static bool TryConstructPacket_LobbyPlayerBusyTrueFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int playerIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_LobbyPlayerBusyTrue(playerIndex);
        return true;
    }
    
    private static bool TryConstructPacket_LobbyPlayerBusyFalseFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int playerIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_LobbyPlayerBusyFalse(playerIndex);
        return true;
    }

    private static bool TryConstructPacket_LobbyNewPlayerFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        byte size = buffer[1];
        if(buffer.Count < 2 + size) return false;
        for(int i = 0; i < 2; ++i) buffer.PopLeft();
        byte[] nameBuffer = new byte[size]; for(int i = 0; i < size; ++i) nameBuffer[i] = buffer.PopLeft();
        string name = nameBuffer.GetStringFromUtf8();
        if(name.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Packet has name with invalid length {name.Length}. It will be trimmed.");
            name = new(name.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        packet = new Packet_LobbyNewPlayer(name);
        return true;
    }
    
    private static bool TryConstructPacket_LobbyDisconnectFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_LobbyDisconnect((DisconnectReasonEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_LobbyDisconnectOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        DisconnectReasonEnum reason = (DisconnectReasonEnum)buffer[1];
        int playerIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_LobbyDisconnectOther(reason, playerIndex);
        return true;
    }
    
    private static bool TryConstructPacket_LobbyTimeoutWarningFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int secondsRemaining = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_LobbyTimeoutWarning(secondsRemaining);
        return true;
    }

    private static bool TryConstructPacket_LobbyTimeoutFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_LobbyTimeout();
        return true;
    }
    
    private static bool TryConstructPacket_ServerClosingFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_ServerClosing();
        return true;
    }
    
    private static bool TryConstructPacket_NewGameStartingFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        GameTurnEnum turn = (GameTurnEnum)buffer[1];
        int opponentIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameStarting(turn, opponentIndex);
        return true;
    }
    
    private static bool TryConstructPacket_GameActionPlaceFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        uint size = new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        if(buffer.Count < 6 + size) return false;
        byte column = buffer[1];
        for(int i = 0; i < 6; ++i) buffer.PopLeft();
        byte[] path = new byte[size]; for(int i = 0; i < size; ++i) path[i] = buffer.PopLeft();
        packet = new Packet_GameActionPlace(column, path.GetStringFromUtf8());
        return true;
    }
    
    private static bool TryConstructPacket_GameActionPlaceOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionPlaceOk();
        return true;
    }
    
    private static bool TryConstructPacket_GameActionPlaceFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_GameActionPlaceFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_GameActionPlaceOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        uint size = new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        if(buffer.Count < 6 + size) return false;
        byte column = buffer[1];
        for(int i = 0; i < 6; ++i) buffer.PopLeft();
        byte[] path = new byte[size]; for(int i = 0; i < size; ++i) path[i] = buffer.PopLeft();
        packet = new Packet_GameActionPlaceOther(column, path.GetStringFromUtf8());
        return true;
    }
    
    private static bool TryConstructPacket_GameActionRefillFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionRefill();
        return true;
    }
    
    private static bool TryConstructPacket_GameActionRefillOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionRefillOk();
        return true;
    }
    
    private static bool TryConstructPacket_GameActionRefillFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_GameActionRefillFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_GameActionRefillOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionRefillOther();
        return true;
    }
    
    private static bool TryConstructPacket_GameQuitFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameQuit();
        return true;
    }
    
    private static bool TryConstructPacket_GameQuitOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameQuitOk();
        return true;
    }
    
    private static bool TryConstructPacket_GameQuitFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_GameQuitFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
    
    private static bool TryConstructPacket_GameQuitOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameQuitOther();
        return true;
    }
    
    private static bool TryConstructPacket_GameFinishedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 10) return false;
        GameResultEnum result = (GameResultEnum)buffer[1];
        int player1Score = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        int player2Score = (int)new[]{buffer[6], buffer[7], buffer[8], buffer[9]}.ReadBigEndian<uint>();
        for(int i = 0; i < 10; ++i) buffer.PopLeft();
        packet = new Packet_GameFinished(result, player1Score, player2Score);
        return true;
    }

    private static bool CreateInvalidPacket(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        PacketTypeEnum type = (PacketTypeEnum)buffer[0];
        GD.PushError($"Unknown packet type {type}");
        buffer.PopLeft();
        packet = new Packet_InvalidPacket(type);
        return true;
    }

    #endregion
}
