using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to tell a client the server received an invalid packet.
/// </summary>
public partial class Packet_InvalidPacketInform : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.INVALID_PACKET_INFORM;

    /// <summary>
    /// The type of received packet
    /// </summary>
    [Export]
    public PacketTypeEnum GivenPacketType{get; private set;}

    public Packet_InvalidPacketInform(PacketTypeEnum givenPacketType)
    {
        GivenPacketType = givenPacketType;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)GivenPacketType, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_InvalidPacketInformFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_InvalidPacketInform((PacketTypeEnum)buffer.PopLeft());
        return true;
    }
}
