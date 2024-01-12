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
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian(LobbyId, index, out _);
        return buffer;
    }
}
