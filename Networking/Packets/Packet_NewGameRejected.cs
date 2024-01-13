using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRejected : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REJECTED;

    [Export]
    public int RequestSourceIndex{get; private set;}
    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameRejected(int requestSourceIndex, int requestTargetIndex)
    {
        RequestSourceIndex = requestSourceIndex;
        RequestTargetIndex = requestTargetIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)RequestSourceIndex, index, out index);
        buffer.WriteBigEndian((uint)RequestTargetIndex, index, out _);
        return buffer;
    }
}
