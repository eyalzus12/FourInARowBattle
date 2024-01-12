using Godot;

namespace FourInARowBattle;

public partial class Packet_InvalidPacketInform : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.INVALID_PACKET_INFORM;

    [Export]
    public PacketTypeEnum GivenPacketType{get; set;}

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
}
