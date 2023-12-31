using Godot;

namespace FourInARowBattle;

public class Packet_GameActionPlaceOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OTHER;

    public byte Column{get; init;}
    public string ScenePath{get; init;} = null!;

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
