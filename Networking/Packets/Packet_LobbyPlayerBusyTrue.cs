namespace FourInARowBattle;

public partial class Packet_LobbyPlayerBusyTrue : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_PLAYER_BUSY_TRUE;

    public int PlayerIndex{get; private set;}

    public Packet_LobbyPlayerBusyTrue(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)PlayerIndex, index, out _);
        return buffer;
    }
}