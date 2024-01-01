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
        if(stringBuffer.Length > byte.MaxValue)
        {
            GD.PushError($"Player name is too long: {OtherPlayerName}");
            stringBuffer = stringBuffer.Take(byte.MaxValue).ToArray();
        }
        byte[] buffer = new byte[1 + 1 + stringBuffer.Length];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU8((byte)stringBuffer.Length, 1);
        buffer.StoreBuffer(stringBuffer, 2);
        return buffer;
    }
}
