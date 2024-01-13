using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST;

    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameRequest(int requestTargetIndex)
    {
        RequestTargetIndex = requestTargetIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)RequestTargetIndex, index, out _);
        return buffer;
    }
}
