using System;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_REQUEST;

    [Export]
    public uint LobbyId{get; private set;}
    [Export]
    public string PlayerName{get; private set;} = null!;

    public Packet_ConnectLobbyRequest(uint lobbyId, string playerName)
    {
        LobbyId = lobbyId;
        PlayerName = playerName;
    }

    public override byte[] ToByteArray()
    {
        if(PlayerName.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Player name has invalid length {PlayerName.Length}");
            PlayerName = new(PlayerName.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        byte[] stringBuffer = PlayerName.ToUtf8Buffer();
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint) + sizeof(byte) + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian(LobbyId, index, out index);
        buffer.WriteBigEndian((byte)stringBuffer.Length, index, out index);
        buffer.WriteBuffer(stringBuffer, index, out _);
        return buffer;
    }
}
