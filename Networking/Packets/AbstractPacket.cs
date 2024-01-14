using DequeNet;
using Godot;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FourInARowBattle;

/// <summary>
/// The base class of all packets
/// </summary>
public abstract partial class AbstractPacket : RefCounted
{
    private delegate bool PacketConstructor(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet);

    // A big dictionary to map packet types to packet creators.
    // Since polymorphism on static functions is impossible, this is the best we can do.
    // Having some unified type (like AbstractFailurePacket) is also not feasible
    // since we can't invoke the constructor generically.

    private static readonly ReadOnlyDictionary<PacketTypeEnum, PacketConstructor> _packetDict =
        new Dictionary<PacketTypeEnum, PacketConstructor>()
        {
            {PacketTypeEnum.DUMMY, Packet_Dummy.TryConstructPacket_DummyFrom},
            {PacketTypeEnum.INVALID_PACKET, Packet_InvalidPacket.TryConstructPacket_InvalidPacketFrom},
            {PacketTypeEnum.INVALID_PACKET_INFORM, Packet_InvalidPacketInform.TryConstructPacket_InvalidPacketInformFrom},
            {PacketTypeEnum.CREATE_LOBBY_REQUEST, Packet_CreateLobbyRequest.TryConstructPacket_CreateLobbyRequestFrom},
            {PacketTypeEnum.CREATE_LOBBY_OK, Packet_CreateLobbyOk.TryConstructPacket_CreateLobbyOkFrom},
            {PacketTypeEnum.CREATE_LOBBY_FAIL, Packet_CreateLobbyFail.TryConstructPacket_CreateLobbyFailFrom},
            {PacketTypeEnum.CONNECT_LOBBY_REQUEST, Packet_ConnectLobbyRequest.TryConstructPacket_ConnectLobbyRequestFrom},
            {PacketTypeEnum.CONNECT_LOBBY_OK, Packet_ConnectLobbyOk.TryConstructPacket_ConnectLobbyOkFrom},
            {PacketTypeEnum.CONNECT_LOBBY_FAIL, Packet_ConnectLobbyFail.TryConstructPacket_ConnectLobbyFailFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST, Packet_NewGameRequest.TryConstructPacket_NewGameRequestFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST_OK, Packet_NewGameRequestOk.TryConstructPacket_NewGameRequestOkFrom},
            {PacketTypeEnum.NEW_GAME_REQUEST_FAIL, Packet_NewGameRequestFail.TryConstructPacket_NewGameRequestFailFrom},
            {PacketTypeEnum.NEW_GAME_REQUESTED, Packet_NewGameRequested.TryConstructPacket_NewGameRequestedFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT, Packet_NewGameAccept.TryConstructPacket_NewGameAcceptFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT_OK, Packet_NewGameAcceptOk.TryConstructPacket_NewGameAcceptOkFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPT_FAIL, Packet_NewGameAcceptFail.TryConstructPacket_NewGameAcceptFailFrom},
            {PacketTypeEnum.NEW_GAME_ACCEPTED, Packet_NewGameAccepted.TryConstructPacket_NewGameAcceptedFrom},
            {PacketTypeEnum.NEW_GAME_REJECT, Packet_NewGameReject.TryConstructPacket_NewGameRejectFrom},
            {PacketTypeEnum.NEW_GAME_REJECT_OK, Packet_NewGameRejectOk.TryConstructPacket_NewGameRejectOkFrom},
            {PacketTypeEnum.NEW_GAME_REJECT_FAIL, Packet_NewGameRejectFail.TryConstructPacket_NewGameRejectFailFrom},
            {PacketTypeEnum.NEW_GAME_REJECTED, Packet_NewGameRejected.TryConstructPacket_NewGameRejectedFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL, Packet_NewGameCancel.TryConstructPacket_NewGameCancelFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL_OK, Packet_NewGameCancelOk.TryConstructPacket_NewGameCancelOkFrom},
            {PacketTypeEnum.NEW_GAME_CANCEL_FAIL, Packet_NewGameCancelFail.TryConstructPacket_NewGameCancelFailFrom},
            {PacketTypeEnum.NEW_GAME_CANCELED, Packet_NewGameCanceled.TryConstructPacket_NewGameCanceledFrom},
            {PacketTypeEnum.LOBBY_PLAYER_BUSY_TRUE, Packet_LobbyPlayerBusyTrue.TryConstructPacket_LobbyPlayerBusyTrueFrom},
            {PacketTypeEnum.LOBBY_PLAYER_BUSY_FALSE, Packet_LobbyPlayerBusyFalse.TryConstructPacket_LobbyPlayerBusyFalseFrom},
            {PacketTypeEnum.LOBBY_NEW_PLAYER, Packet_LobbyNewPlayer.TryConstructPacket_LobbyNewPlayerFrom},
            {PacketTypeEnum.LOBBY_DISCONNECT, Packet_LobbyDisconnect.TryConstructPacket_LobbyDisconnectFrom},
            {PacketTypeEnum.LOBBY_DISCONNECT_OTHER, Packet_LobbyDisconnectOther.TryConstructPacket_LobbyDisconnectOtherFrom},
            {PacketTypeEnum.LOBBY_TIMEOUT_WARNING, Packet_LobbyTimeoutWarning.TryConstructPacket_LobbyTimeoutWarningFrom},
            {PacketTypeEnum.LOBBY_TIMEOUT, Packet_LobbyTimeout.TryConstructPacket_LobbyTimeoutFrom},
            {PacketTypeEnum.SERVER_CLOSING, Packet_ServerClosing.TryConstructPacket_ServerClosingFrom},
            {PacketTypeEnum.NEW_GAME_STARTING, Packet_NewGameStarting.TryConstructPacket_NewGameStartingFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE, Packet_GameActionPlace.TryConstructPacket_GameActionPlaceFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_OK, Packet_GameActionPlaceOk.TryConstructPacket_GameActionPlaceOkFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_FAIL, Packet_GameActionPlaceFail.TryConstructPacket_GameActionPlaceFailFrom},
            {PacketTypeEnum.GAME_ACTION_PLACE_OTHER, Packet_GameActionPlaceOther.TryConstructPacket_GameActionPlaceOtherFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL, Packet_GameActionRefill.TryConstructPacket_GameActionRefillFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_OK, Packet_GameActionRefillOk.TryConstructPacket_GameActionRefillOkFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_FAIL, Packet_GameActionRefillFail.TryConstructPacket_GameActionRefillFailFrom},
            {PacketTypeEnum.GAME_ACTION_REFILL_OTHER, Packet_GameActionRefillOther.TryConstructPacket_GameActionRefillOtherFrom},
            {PacketTypeEnum.GAME_QUIT, Packet_GameQuit.TryConstructPacket_GameQuitFrom},
            {PacketTypeEnum.GAME_QUIT_OK, Packet_GameQuitOk.TryConstructPacket_GameQuitOkFrom},
            {PacketTypeEnum.GAME_QUIT_FAIL, Packet_GameQuitFail.TryConstructPacket_GameQuitFailFrom},
            {PacketTypeEnum.GAME_QUIT_OTHER, Packet_GameQuitOther.TryConstructPacket_GameQuitOtherFrom},
            {PacketTypeEnum.GAME_FINISHED, Packet_GameFinished.TryConstructPacket_GameFinishedFrom},
        }.AsReadOnly();

    /// <summary>
    /// Packet type getter
    /// </summary>
    /// <value>The type of packet</value>
    public abstract PacketTypeEnum PacketType{get;}

    /// <summary>
    /// Store the packet into a byte array
    /// </summary>
    /// <returns>The byte array</returns>
    public abstract byte[] ToByteArray();

    
    /// <summary>
    /// Attempt to construct a packet from the buffer
    /// If there's a full valid packet, true is returned, packet is the resulting packet, and the used data is taken out of buffer
    /// If there isn't a full valid packet, false is returned and packet is null
    /// </summary>
    /// <param name="buffer">The buffer to read from</param>
    /// <param name="packet">The out param for the packet</param>
    /// <returns>Whether construction succeeded</returns>
    public static bool TryConstructPacketFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        packet = null;
        if(buffer.Count < 1) return false;
        return _packetDict.GetValueOrDefault((PacketTypeEnum)buffer[0], CreateInvalidPacket)(buffer, out packet);
    }

    /// <summary>
    /// Create Packet_InvalidPacket. Used when an invalid packet type was received.
    /// </summary>
    /// <param name="buffer">The buffer to create from</param>
    /// <param name="packet">The resulting packet</param>
    /// <returns>True</returns>
    private static bool CreateInvalidPacket(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        PacketTypeEnum type = (PacketTypeEnum)buffer[0];
        GD.PushError($"Unknown packet type {type}");
        buffer.PopLeft();
        packet = new Packet_InvalidPacket(type);
        return true;
    }
}
