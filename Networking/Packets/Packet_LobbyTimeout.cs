using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_LobbyTimeout : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_TIMEOUT;

    public Packet_LobbyTimeout()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }

    public static bool TryConstructPacket_LobbyTimeoutFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_LobbyTimeout();
        return true;
    }
}
