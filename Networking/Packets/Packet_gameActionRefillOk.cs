namespace FourInARowBattle;

public partial class Packet_GameActionRefillOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_OK;

    public Packet_GameActionRefillOk()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}
