using System;
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
        if(stringBuffer.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.PushError($"Player name has invalid length {stringBuffer.Length}");
            stringBuffer = stringBuffer.Take(Globals.NAME_LENGTH_LIMIT).ToArray();
        }
        byte[] buffer = new byte[1 + 4 + 1 + stringBuffer.Length];
        int index = 0;
        index += buffer.WriteBigEndian<byte>((byte)PacketType, index);
        index += buffer.WriteBigEndian<uint>(LobbyId, index);
        index += buffer.WriteBigEndian<byte>((byte)stringBuffer.Length, index);
        buffer.StoreBuffer(stringBuffer, index);
        return buffer;
    }
}
