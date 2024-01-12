using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameAccepted : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPTED;

    [Export]
    public int RequestSourceIndex{get; set;}
    [Export]
    public int RequestTargetIndex{get; set;}

    public Packet_NewGameAccepted(int requestSourceIndex, int requestTargetIndex)
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
