namespace FourInARowBattle;

public partial class Packet_NewGameAccept : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPT;

    public Packet_NewGameAccept()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}
