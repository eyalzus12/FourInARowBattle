namespace FourInARowBattle;

public class Packet_Dummy : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.DUMMY;

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        return buffer;
    }
}
