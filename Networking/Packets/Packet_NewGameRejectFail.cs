using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRejectFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECT_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}
    [Export]
    public int RequestSourceIndex{get; set;}

    public Packet_NewGameRejectFail(ErrorCodeEnum errorCode, int requestSourceIndex)
    {
        ErrorCode = errorCode;
        RequestSourceIndex = requestSourceIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)ErrorCode, index, out index);
        buffer.WriteBigEndian((uint)RequestSourceIndex, index, out _);
        return buffer;
    }
}
