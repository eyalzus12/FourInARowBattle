using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// An unused packet type. Theoretically used by the server to finish the game and tell the players the result.
/// </summary>
public partial class Packet_GameFinished : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_FINISHED;

    [Export]
    public GameResultEnum Result{get; private set;}
    [Export]
    public int Player1Score{get; private set;}
    [Export]
    public int Player2Score{get; private set;}

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

    public static bool TryConstructPacket_GameFinishedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 10) return false;
        GameResultEnum result = (GameResultEnum)buffer[1];
        int player1Score = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        int player2Score = (int)new[]{buffer[6], buffer[7], buffer[8], buffer[9]}.ReadBigEndian<uint>();
        for(int i = 0; i < 10; ++i) buffer.PopLeft();
        packet = new Packet_GameFinished(result, player1Score, player2Score);
        return true;
    }
}
