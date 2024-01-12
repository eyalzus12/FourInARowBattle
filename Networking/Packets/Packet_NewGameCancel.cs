namespace FourInARowBattle;

public partial class Packet_NewGameCancel : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCEL;

    public Packet_NewGameCancel()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        return buffer;
    }
}
