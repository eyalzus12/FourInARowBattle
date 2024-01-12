namespace FourInARowBattle;

public partial class Packet_NewGameRequested : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUESTED;

    public Packet_NewGameRequested()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        return buffer;
    }
}
