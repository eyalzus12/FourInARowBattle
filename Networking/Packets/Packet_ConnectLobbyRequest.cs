using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_REQUEST;

    [Export]
    public uint LobbyId{get; set;}
    [Export]
    public string PlayerName{get; set;} = null!;

    public Packet_ConnectLobbyRequest(uint lobbyId, string playerName)
    {
        LobbyId = lobbyId;
        PlayerName = playerName;
    }

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = PlayerName.ToUtf8Buffer();
        if(stringBuffer.Length > byte.MaxValue)
        {
            GD.PushError($"Player name is too long: {PlayerName}");
            stringBuffer = stringBuffer.Take(byte.MaxValue).ToArray();
        }
        byte[] buffer = new byte[1 + 4 + 1 + stringBuffer.Length];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU32(LobbyId, 1);
        buffer.StoreBigEndianU8((byte)stringBuffer.Length, 5);
        buffer.StoreBuffer(stringBuffer, 6);
        return buffer;
    }
}
