using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyDisconnectOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT_OTHER;

    [Export]
    public DisconnectReasonEnum Reason{get; set;}

    public Packet_LobbyDisconnectOther(DisconnectReasonEnum reason)
    {
        Reason = reason;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((byte)Reason, 1);
        return buffer;
    }
}
