using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DequeNet;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the client to request connecting to a lobby
/// </summary>
public partial class Packet_ConnectLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_REQUEST;

    /// <summary>
    /// The id of the desired lobby
    /// </summary>
    [Export]
    public uint LobbyId{get; private set;}
    /// <summary>
    /// The name of the joining player
    /// </summary>
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

    public static bool TryConstructPacket_ConnectLobbyRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        byte size = buffer[5];
        if(buffer.Count < 6 + size) return false;
        uint lobbyId = new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++i) buffer.PopLeft();
        byte[] nameBuffer = new byte[size]; for(int i = 0; i < size; ++i) nameBuffer[i] = buffer.PopLeft();
        string name = nameBuffer.GetStringFromUtf8();
        if(name.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Packet has name with invalid length {name.Length}. It will be trimmed.");
            name = new(name.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        packet = new Packet_ConnectLobbyRequest(lobbyId, name);
        return true;
    }
}
