
namespace FourInARowBattle;

public class Packet_CreateLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_OK;

    public uint LobbyId{get; init;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 4];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU32(LobbyId, buffer, 1);
        return buffer;
    }
}
