using System.Diagnostics.CodeAnalysis;
using DequeNet;
using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_OK;

    [Export]
    public uint LobbyId{get; private set;}

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
    
    public static bool TryConstructPacket_CreateLobbyOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        uint lobbyId = new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_CreateLobbyOk(lobbyId);
        return true;
    }
}
