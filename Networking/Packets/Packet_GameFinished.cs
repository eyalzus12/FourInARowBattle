using Godot;

namespace FourInARowBattle;

public partial class Packet_GameFinished : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_FINISHED;

    [Export]
    public GameResultEnum Result{get; set;}
    [Export]
    public int Player1Score{get; set;}
    [Export]
    public int Player2Score{get; set;}

    public Packet_GameFinished(GameResultEnum result, int player1Score, int player2Score)
    {
        Result = result;
        Player1Score = player1Score;
        Player2Score = player2Score;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)Result, index, out index);
        buffer.WriteBigEndian((uint)Player1Score, index, out index);
        buffer.WriteBigEndian((uint)Player2Score, index, out _);
        return buffer;
    }
}
