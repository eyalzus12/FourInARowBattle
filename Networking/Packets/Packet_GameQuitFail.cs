using System.Diagnostics.CodeAnalysis;
using DequeNet;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate that quitting the game failed
/// </summary>
public partial class Packet_GameQuitFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_QUIT_FAIL;

    /// <summary>
    /// The failure error code
    /// </summary>
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

    public static bool TryConstructPacket_GameQuitFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_GameQuitFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
}
