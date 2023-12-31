namespace FourInARowBattle;

public class Packet_GameActionRefillOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_OTHER;

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        return buffer;
    }
}
