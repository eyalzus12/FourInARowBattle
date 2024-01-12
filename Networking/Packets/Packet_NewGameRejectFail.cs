using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRejectFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECT_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}

    public Packet_NewGameRejectFail(ErrorCodeEnum errorCode)
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
