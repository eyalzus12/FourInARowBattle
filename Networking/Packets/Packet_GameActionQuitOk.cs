namespace FourInARowBattle;

public partial class Packet_GameQuitOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_QUIT_OK;

    public Packet_GameQuitOk()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}
