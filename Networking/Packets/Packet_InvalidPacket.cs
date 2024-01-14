using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_InvalidPacket : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.INVALID_PACKET;

    [Export]
    public PacketTypeEnum GivenPacketType{get; private set;}

    public Packet_InvalidPacket(PacketTypeEnum givenPacketType)
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
    public static bool TryConstructPacket_InvalidPacketFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        GD.PushWarning("Received packet type INVALID_PACKET, but that packet type is for internal use only. Use INVALID_PACKET_INFORM to respond to an invalid packet");
        buffer.PopLeft();
        packet = new Packet_InvalidPacket(PacketTypeEnum.INVALID_PACKET);
        return true;
    }
}
