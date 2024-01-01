namespace FourInARowBattle;

public partial class Packet_Dummy : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.DUMMY;

    public Packet_Dummy()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}
