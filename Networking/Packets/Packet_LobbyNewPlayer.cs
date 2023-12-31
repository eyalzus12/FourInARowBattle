using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyNewPlayer : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_NEW_PLAYER;

    [Export]
    public string OtherPlayerName{get; set;} = null!;

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = OtherPlayerName.ToUtf8Buffer();
        if(stringBuffer.Length > byte.MaxValue)
        {
            GD.PushError($"Player name is too long: {OtherPlayerName}");
            stringBuffer = stringBuffer.Take(byte.MaxValue).ToArray();
        }
        byte[] buffer = new byte[1 + 1 + stringBuffer.Length];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)stringBuffer.Length, buffer, 1);
        Utils.StoreBuffer(stringBuffer, buffer, 2);
        return buffer;
    }
}
