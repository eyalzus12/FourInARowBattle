using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate connecting to the server failed
/// </summary>
public partial class Packet_ConnectLobbyFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_FAIL;

    /// <summary>
    /// The failure error code
    /// </summary>
    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}

    public Packet_ConnectLobbyFail(ErrorCodeEnum errorCode)
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

    public static bool TryConstructPacket_ConnectLobbyFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        buffer.PopLeft();
        packet = new Packet_ConnectLobbyFail((ErrorCodeEnum)buffer.PopLeft());
        return true;
    }
}
