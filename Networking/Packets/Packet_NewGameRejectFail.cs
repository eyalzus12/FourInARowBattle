using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_NewGameRejectFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECT_FAIL;

    [Export]
    public ErrorCodeEnum ErrorCode{get; private set;}
    [Export]
    public int RequestSourceIndex{get; private set;}

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
    
    public static bool TryConstructPacket_NewGameRejectFailFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        ErrorCodeEnum errorCode = (ErrorCodeEnum)buffer[1];
        int sourceIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRejectFail(errorCode, sourceIndex);
        return true;
    }
}
