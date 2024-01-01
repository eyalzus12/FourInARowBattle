using System;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_OK;

    [Export]
    public string OtherPlayerName{get; set;} = "";

    public Packet_ConnectLobbyOk(string? otherPlayerName)
    {
        OtherPlayerName = otherPlayerName ?? "";
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
        buffer.StoreBigEndianU8((byte)PacketTypeEnum.CONNECT_LOBBY_OK, 0);
        buffer.StoreBigEndianU32((uint)stringBuffer.Length, 1);
        buffer.StoreBuffer(stringBuffer, 5);
        return buffer;
    }
}
