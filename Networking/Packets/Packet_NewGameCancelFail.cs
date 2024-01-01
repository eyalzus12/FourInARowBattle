using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameCancelFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCEL_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}

    public Packet_NewGameCancelFail(ErrorCodeEnum errorCode)
    {
        ErrorCode = errorCode;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU8((byte)ErrorCode, 1);
        return buffer;
    }
}
