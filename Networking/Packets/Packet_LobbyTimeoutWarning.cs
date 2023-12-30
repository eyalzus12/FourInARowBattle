namespace FourInARowBattle;

public class Packet_LobbyTimeoutWarning : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_TIMEOUT_WARNING;

    public uint SecondsRemaining{get; init;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 4];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU32(SecondsRemaining, buffer, 1);
        return buffer;
    }
}
