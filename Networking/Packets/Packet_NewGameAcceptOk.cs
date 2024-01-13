using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameAcceptOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPT_OK;

    [Export]
    public int RequestSourceIndex{get; private set;}

    public Packet_NewGameAcceptOk(int requestSourceIndex)
    {
        RequestSourceIndex = requestSourceIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)RequestSourceIndex, index, out _);
        return buffer;
    }
}
