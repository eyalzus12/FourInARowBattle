using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameCancelFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCEL_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}
    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameCancelFail(ErrorCodeEnum errorCode, int requestTargetIndex)
    {
        ErrorCode = errorCode;
        RequestTargetIndex = requestTargetIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)ErrorCode, index, out index);
        buffer.WriteBigEndian((uint)RequestTargetIndex, index, out _);
        return buffer;
    }
}
