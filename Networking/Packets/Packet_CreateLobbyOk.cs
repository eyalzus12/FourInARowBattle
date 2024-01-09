using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_OK;

    [Export]
    public uint LobbyId{get; set;}

    public Packet_CreateLobbyOk(uint lobbyId)
    {
        LobbyId = lobbyId;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 4];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU32(LobbyId, 1);
        return buffer;
    }
}
