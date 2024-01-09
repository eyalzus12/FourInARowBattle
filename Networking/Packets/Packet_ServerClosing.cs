namespace FourInARowBattle;

public partial class Packet_ServerClosing : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.SERVER_CLOSING;

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}