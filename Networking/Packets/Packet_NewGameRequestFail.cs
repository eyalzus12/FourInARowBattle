using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate that challenging to a game failed
/// </summary>
public partial class Packet_NewGameRequestFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST_FAIL;

    /// <summary>
    /// Failure error code
    /// </summary>
    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}
    /// <summary>
    /// The index of player that was challenged
    /// </summary>
    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameRequestFail(ErrorCodeEnum errorCode, int requestTargetIndex)
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
    
    public static bool TryConstructPacket_NewGameRequestFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int targetIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequestFail(errorCode, targetIndex);
        return true;
    }
}
