using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameStarting : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_STARTING;

    [Export]
    public int Player1Index{get; private set;}
    [Export]
    public int Player2Index{get; private set;}

    public Packet_NewGameStarting(int player1Index, int player2Index)
    {
        Player1Index = player1Index;
        Player2Index = player2Index;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) +sizeof(uint) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)Player1Index, index, out index);
        buffer.WriteBigEndian((uint)Player2Index, index, out _);
        return buffer;
    }
}
