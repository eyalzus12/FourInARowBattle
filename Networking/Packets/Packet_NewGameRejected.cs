namespace FourInARowBattle;

public partial class Packet_NewGameRejected : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECTED;

    public Packet_NewGameRejected()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}
