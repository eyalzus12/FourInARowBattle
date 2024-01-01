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
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}
