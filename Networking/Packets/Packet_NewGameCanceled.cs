namespace FourInARowBattle;

public partial class Packet_NewGameCanceled : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCELED;

    public Packet_NewGameCanceled()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}
