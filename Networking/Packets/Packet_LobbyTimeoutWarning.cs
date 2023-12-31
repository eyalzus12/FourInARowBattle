using Godot;

namespace FourInARowBattle;

public class Packet_LobbyTimeoutWarning : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_TIMEOUT_WARNING;

    [Export]
    public int SecondsRemaining{get; set;}

    public Packet_LobbyTimeoutWarning(int secondsRemaining)
    {
        SecondsRemaining = secondsRemaining;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 4];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU32((uint)SecondsRemaining, buffer, 1);
        return buffer;
    }
}
