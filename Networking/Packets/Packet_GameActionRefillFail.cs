using System.Diagnostics.CodeAnalysis;
using DequeNet;
using Godot;

namespace FourInARowBattle;

public partial class Packet_GameActionRefillFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}

    public Packet_GameActionRefillFail(ErrorCodeEnum errorCode)
    {
        ErrorCode = errorCode;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)ErrorCode, index, out _);
        return buffer;
    }
    
    public static bool TryConstructPacket_GameActionRefillFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_GameActionRefillFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
}
