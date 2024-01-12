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
        byte[] buffer = new byte[1 + 1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((byte)GivenPacketType, 1);
        return buffer;
    }
}
