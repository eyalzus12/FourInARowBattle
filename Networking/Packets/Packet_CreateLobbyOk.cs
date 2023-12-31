
using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_OK;

    [Export]
    public uint LobbyId{get; set;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 4];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU32(LobbyId, buffer, 1);
        return buffer;
    }
}
