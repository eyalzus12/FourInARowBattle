using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_REQUEST;

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
}
