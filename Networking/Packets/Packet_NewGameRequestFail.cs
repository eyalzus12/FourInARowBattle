using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRequestFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}

    public Packet_NewGameRequestFail(ErrorCodeEnum errorCode)
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
