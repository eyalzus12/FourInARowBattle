namespace FourInARowBattle;

public partial class Packet_LobbyTimeout : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_TIMEOUT;

    public Packet_LobbyTimeout()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        return buffer;
    }
}
