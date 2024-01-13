using Godot;

namespace FourInARowBattle;

public partial class Packet_GameActionPlace : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE;

    [Export]
    public byte Column{get; private set;}
    [Export]
    public string ScenePath{get; private set;} = null!;

    public Packet_GameActionPlace(byte column, string scenePath)
    {
        Column = column;
        ScenePath = scenePath;
    }

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = ScenePath.ToUtf8Buffer();
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint) + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian(Column, index, out index);
        buffer.WriteBigEndian((uint)stringBuffer.Length, index, out index);
        buffer.WriteBuffer(stringBuffer, index, out _);
        return buffer;
    }
}
