using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameRequestOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST_OK;

    [Export]
    public int RequestTargetIndex{get; set;}

    public Packet_NewGameRequestOk(int requestTargetIndex)
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
