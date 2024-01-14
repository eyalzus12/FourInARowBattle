using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate it is closing
/// </summary>
public partial class Packet_ServerClosing : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.SERVER_CLOSING;

    public Packet_ServerClosing()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }

    public static bool TryConstructPacket_ServerClosingFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_ServerClosing();
        return true;
    }
}