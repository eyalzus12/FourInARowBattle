using Godot;

namespace FourInARowBattle;

public partial class Packet_GameActionPlaceOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OTHER;

    [Export]
    public byte Column{get; set;}
    [Export]
    public string ScenePath{get; set;} = null!;

    public Packet_GameActionPlaceOther(byte column, string scenePath)
    {
        Column = column;
        ScenePath = scenePath;
    }

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = ScenePath.ToUtf8Buffer();
        byte[] buffer = new byte[1 + 1 + 4 + stringBuffer.Length];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU8(Column, 1);
        buffer.StoreBigEndianU32((uint)stringBuffer.Length, 2);
        buffer.StoreBuffer(stringBuffer, 8);
        return buffer;
    }
}
