using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyTimeoutWarning : AbstractPacket
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
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((uint)SecondsRemaining, 1);
        return buffer;
    }
}
