
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; set;}

    public Packet_ConnectLobbyFail(ErrorCodeEnum errorCode)
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
