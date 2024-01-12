
using Godot;

namespace FourInARowBattle;

public partial class Packet_GameActionRefillFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}

    public Packet_GameActionRefillFail(ErrorCodeEnum errorCode)
    {
        ErrorCode = errorCode;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((byte)ErrorCode, 1);
        return buffer;
    }
}
