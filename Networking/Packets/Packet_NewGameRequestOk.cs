namespace FourInARowBattle;

public partial class Packet_NewGameRequestOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST_OK;

    public Packet_NewGameRequestOk()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}
