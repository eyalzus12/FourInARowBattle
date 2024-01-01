namespace FourInARowBattle;

public partial class Packet_GameActionPlaceOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OK;

    public Packet_GameActionPlaceOk()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}