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
        byte[] buffer = new byte[1 + 1 + 4 + 4];
        buffer.WriteBigEndian((byte)PacketType, 0);
        buffer.WriteBigEndian((byte)Result, 1);
        buffer.WriteBigEndian((uint)Player1Score, 2);
        buffer.WriteBigEndian((uint)Player2Score, 6);
        return buffer;
    }
}
