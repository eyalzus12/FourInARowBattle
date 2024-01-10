using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_CreateLobbyRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CREATE_LOBBY_REQUEST;

    [Export]
    public string PlayerName{get; set;} = null!;

    public Packet_CreateLobbyRequest(string playerName)
    {
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
        byte[] buffer = new byte[1 + 1 + stringBuffer.Length];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU8((byte)stringBuffer.Length, 1);
        buffer.StoreBuffer(stringBuffer, 2);
        return buffer;
    }
}
