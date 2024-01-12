using Godot;

namespace FourInARowBattle;

public partial class Packet_LobbyDisconnectOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_DISCONNECT_OTHER;

    [Export]
    public DisconnectReasonEnum Reason{get; set;}
    [Export]
    public int PlayerIndex{get; set;}

    public Packet_LobbyDisconnectOther(DisconnectReasonEnum reason, int playerIndex)
    {
        Reason = reason;
        PlayerIndex = playerIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Reason, index, out index);
        buffer.WriteBigEndian((uint)PlayerIndex, index, out _);
        return buffer;
    }
}
