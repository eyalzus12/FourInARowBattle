using System.Diagnostics.CodeAnalysis;
using DequeNet;

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

    public static bool TryConstructPacket_GameQuitOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameQuitOther();
        return true;
    }
}