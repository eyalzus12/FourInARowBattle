namespace FourInARowBattle;

public partial class Packet_GameActionRefill : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL;

    public Packet_GameActionRefill()
    {

    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        return buffer;
    }
}
