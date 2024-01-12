namespace FourInARowBattle;

public partial class Packet_Dummy : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.DUMMY;

    public Packet_Dummy()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}
