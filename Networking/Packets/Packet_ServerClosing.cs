namespace FourInARowBattle;

public partial class Packet_ServerClosing : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.SERVER_CLOSING;

    public Packet_ServerClosing()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}