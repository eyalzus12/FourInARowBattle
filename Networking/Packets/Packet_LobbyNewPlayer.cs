using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyNewPlayer : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_NEW_PLAYER;

    [Export]
    public string OtherPlayerName{get; private set;} = null!;

    public Packet_LobbyNewPlayer(string otherPlayerName)
    {
        OtherPlayerName = otherPlayerName;
    }

    public override byte[] ToByteArray()
    {
        if(OtherPlayerName.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.Print($"Player name has invalid length {OtherPlayerName.Length}");
            OtherPlayerName = new(OtherPlayerName.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
        }
        byte[] stringBuffer = OtherPlayerName.ToUtf8Buffer();
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)stringBuffer.Length, index, out index);
        buffer.WriteBuffer(stringBuffer, index, out _);
        return buffer;
    }
}
