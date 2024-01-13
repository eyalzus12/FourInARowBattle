using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameStarting : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_STARTING;

    [Export]
    public GameTurnEnum Turn{get; private set;}
    [Export]
    public int OpponentIndex{get; private set;}

    public Packet_NewGameStarting(GameTurnEnum turn, int opponentIndex)
    {
        Turn = turn;
        OpponentIndex = opponentIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Turn, index, out index);
        buffer.WriteBigEndian((uint)OpponentIndex, index, out _);
        return buffer;
    }
}
