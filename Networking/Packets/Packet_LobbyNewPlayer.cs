using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyNewPlayer : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_NEW_PLAYER;

    [Export]
    public string OtherPlayerName{get; set;} = null!;

    public Packet_LobbyNewPlayer(string otherPlayerName)
    {
        OtherPlayerName = otherPlayerName;
    }

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = OtherPlayerName.ToUtf8Buffer();
        if(stringBuffer.Length > Globals.NAME_LENGTH_LIMIT)
        {
            GD.PushError($"Player name has invalid length {stringBuffer.Length}");
            stringBuffer = stringBuffer.Take(Globals.NAME_LENGTH_LIMIT).ToArray();
        }
        byte[] buffer = new byte[1 + 1 + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((byte)stringBuffer.Length, 1);
        buffer.StoreBuffer(stringBuffer, 2);
        return buffer;
    }
}
