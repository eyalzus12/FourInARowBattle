
using Godot;

namespace FourInARowBattle;

public partial class Packet_GameQuitFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_QUIT_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}

    public Packet_GameQuitFail(ErrorCodeEnum errorCode)
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
}
