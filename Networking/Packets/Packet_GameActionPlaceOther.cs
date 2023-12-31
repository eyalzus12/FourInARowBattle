using Godot;

namespace FourInARowBattle;

public partial class Packet_GameActionPlaceOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OTHER;

    [Export]
    public byte Column{get; set;}
    [Export]
    public string ScenePath{get; set;} = null!;

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = ScenePath.ToUtf8Buffer();
        byte[] buffer = new byte[1 + 1 + 4 + stringBuffer.Length];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8(Column, buffer, 1);
        Utils.StoreBigEndianU32((uint)stringBuffer.Length, buffer, 2);
        Utils.StoreBuffer(stringBuffer, buffer, 8);
        return buffer;
    }
}
