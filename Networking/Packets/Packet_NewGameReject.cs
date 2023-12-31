namespace FourInARowBattle;

public partial class Packet_NewGameReject : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECT;

    public Packet_NewGameReject()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        return buffer;
    }
}
