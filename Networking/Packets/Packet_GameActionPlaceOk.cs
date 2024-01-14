using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_GameActionPlaceOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OK;

    public Packet_GameActionPlaceOk()
    {

    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out _);
        return buffer;
    }

    public static bool TryConstructPacket_GameActionPlaceOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        buffer.PopLeft();
        packet = new Packet_GameActionPlaceOk();
        return true;
    }
}