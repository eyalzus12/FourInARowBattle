using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_REQUEST;

    [Export]
    public string PlayerName{get; set;} = null!;

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = PlayerName.ToUtf8Buffer();
        if(stringBuffer.Length > byte.MaxValue)
        {
            GD.PushError($"Player name is too long: {PlayerName}");
            stringBuffer = stringBuffer.Take(byte.MaxValue).ToArray();
        }
        byte[] buffer = new byte[1 + 1 + stringBuffer.Length];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)stringBuffer.Length, buffer, 1);
        Utils.StoreBuffer(stringBuffer, buffer, 2);
        return buffer;
    }
}
