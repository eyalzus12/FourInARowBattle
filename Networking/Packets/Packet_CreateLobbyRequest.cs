using System.Linq;
using System.Diagnostics.CodeAnalysis;
using DequeNet;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the client to request lobby creation from the server
/// </summary>
public partial class Packet_CreateLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_REQUEST;

    /// <summary>
    /// The name of the lobby creator
    /// </summary>
    [Export]
    public string PlayerName{get; private set;} = null!;

    public Packet_CreateLobbyRequest(string playerName)
    {
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
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)stringBuffer.Length, index, out index);
        buffer.WriteBuffer(stringBuffer, index, out _);
        return buffer;
    }
    public static bool TryConstructPacket_CreateLobbyRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 2) return false;
        byte size = buffer[1];
        if(buffer.Count < 2 + size) return false;
        for(int i = 0; i < 2; ++i) buffer.PopLeft();
        byte[] nameBuffer = new byte[size]; for(int i = 0; i < size; ++i) nameBuffer[i] = buffer.PopLeft();
        string name = nameBuffer.GetStringFromUtf8();
        if(name.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Packet has name with invalid length {name.Length}. It will be trimmed.");
            name = new(name.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        packet = new Packet_CreateLobbyRequest(name);
        return true;
    }
}
