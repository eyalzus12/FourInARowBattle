using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyDisconnect : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT;

    [Export]
    public DisconnectReasonEnum Reason{get; private set;}

    public Packet_LobbyDisconnect(DisconnectReasonEnum reason)
    {
        Reason = reason;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Reason, index, out _);
        return buffer;
    }
}
