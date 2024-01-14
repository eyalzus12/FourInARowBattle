using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to tell a client their opponent refilled their tokens
/// </summary>
public partial class Packet_GameActionRefillOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_OTHER;

    public Packet_GameActionRefillOther()
    {
        
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }
    
    public static bool TryConstructPacket_GameActionRefillOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionRefillOther();
        return true;
    }
}
