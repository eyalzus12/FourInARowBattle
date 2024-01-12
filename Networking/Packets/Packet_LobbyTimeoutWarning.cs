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
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)SecondsRemaining, index, out _);
        return buffer;
    }
}
