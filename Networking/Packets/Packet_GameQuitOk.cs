using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_GameQuitOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_QUIT_OK;

    public Packet_GameQuitOk()
    {

    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }

    public static bool TryConstructPacket_GameQuitOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameQuitOk();
        return true;
    }
}
