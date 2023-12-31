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

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1 + 4 + 4];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)Result, buffer, 1);
        Utils.StoreBigEndianU32((uint)Player1Score, buffer, 2);
        Utils.StoreBigEndianU32((uint)Player2Score, buffer, 6);
        return buffer;
    }
}
