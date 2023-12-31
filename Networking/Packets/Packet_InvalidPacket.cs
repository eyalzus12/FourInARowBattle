namespace FourInARowBattle;

public class Packet_InvalidPacket : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.INVALID_PACKET;

    public PacketTypeEnum GivenPacketType{get; init;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)GivenPacketType, buffer, 1);
        return buffer;
    }
}
