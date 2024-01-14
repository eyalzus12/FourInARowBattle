using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to tell clients that a player left a lobby
/// </summary>
public partial class Packet_LobbyDisconnectOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT_OTHER;

    /// <summary>
    /// The reason for disconnecting
    /// </summary>
    [Export]
    public DisconnectReasonEnum Reason{get; private set;}
    /// <summary>
    /// The index of the disconnected player inside the lobby
    /// </summary>
    [Export]
    public int PlayerIndex{get; private set;}

    public Packet_LobbyDisconnectOther(DisconnectReasonEnum reason, int playerIndex)
    {
        Reason = reason;
        PlayerIndex = playerIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Reason, index, out index);
        buffer.WriteBigEndian((uint)PlayerIndex, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_LobbyDisconnectOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        DisconnectReasonEnum reason = (DisconnectReasonEnum)buffer[1];
        int playerIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_LobbyDisconnectOther(reason, playerIndex);
        return true;
    }
}
