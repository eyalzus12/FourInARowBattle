namespace FourInARowBattle;

public partial class Packet_NewGameAccept : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPT;

    public Packet_NewGameAccept()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        return buffer;
    }
}
