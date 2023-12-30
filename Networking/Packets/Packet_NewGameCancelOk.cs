namespace FourInARowBattle;

public class Packet_NewGameCancelOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCEL_OK;

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        return buffer;
    }
}
