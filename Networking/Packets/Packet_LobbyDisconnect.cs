using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the client to disconnect from a lobby
/// </summary>
public partial class Packet_LobbyDisconnect : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT;

    /// <summary>
    /// The reason for disconnecting
    /// </summary>
    [Export]
    public DisconnectReasonEnum Reason{get; private set;}

    public Packet_LobbyDisconnect(DisconnectReasonEnum reason)
    {
        Reason = reason;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Reason, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_LobbyDisconnectFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_LobbyDisconnect((DisconnectReasonEnum)buffer.PopLeft());
        return true;
    }
}
