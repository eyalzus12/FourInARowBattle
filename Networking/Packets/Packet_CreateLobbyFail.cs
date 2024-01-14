using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate creating lobby failed
/// </summary>
public partial class Packet_CreateLobbyFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_FAIL;

    /// <summary>
    /// The failure error code
    /// </summary>
    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}

    public Packet_CreateLobbyFail(ErrorCodeEnum errorCode)
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

    public static bool TryConstructPacket_CreateLobbyFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_CreateLobbyFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
}
