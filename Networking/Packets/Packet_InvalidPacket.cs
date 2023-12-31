using Godot;

namespace FourInARowBattle;

public partial class Packet_InvalidPacket : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.INVALID_PACKET;

    [Export]
    public PacketTypeEnum GivenPacketType{get; set;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)GivenPacketType, buffer, 1);
        return buffer;
    }
}
