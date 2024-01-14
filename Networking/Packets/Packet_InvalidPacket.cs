using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;
using System;

namespace FourInARowBattle;

/// <summary>
/// A packet used internally to indicate a packet with an invalid type was received.
/// </summary>
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
        throw new NotSupportedException("Do not send InvalidPacket. Used InvalidPacketInform to inform of an invalid packet that was received");
    }

    public static bool TryConstructPacket_InvalidPacketFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        GD.PushWarning("Received packet type INVALID_PACKET, but that packet type is for internal use only. Use INVALID_PACKET_INFORM to respond to an invalid packet");
        buffer.PopLeft();
        packet = new Packet_InvalidPacket(PacketTypeEnum.INVALID_PACKET);
        return true;
    }
}
