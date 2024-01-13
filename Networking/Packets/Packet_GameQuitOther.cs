namespace FourInARowBattle;

public partial class Packet_GameQuitOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_QUIT_OTHER;

    public Packet_GameQuitOther()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
}