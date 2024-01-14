using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_GameActionRefillOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_OK;

    public Packet_GameActionRefillOk()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }

    public static bool TryConstructPacket_GameActionRefillOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionRefillOk();
        return true;
    }
}
