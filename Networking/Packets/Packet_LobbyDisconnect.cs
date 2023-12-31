using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyDisconnect : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT;

    [Export]
    public DisconnectReasonEnum Reason{get; set;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)Reason, buffer, 1);
        return buffer;
    }
}
