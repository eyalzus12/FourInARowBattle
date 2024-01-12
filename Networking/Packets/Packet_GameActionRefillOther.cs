namespace FourInARowBattle;

public partial class Packet_GameActionRefillOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_OTHER;

    public Packet_GameActionRefillOther()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        return buffer;
    }
}
