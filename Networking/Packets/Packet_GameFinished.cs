namespace FourInARowBattle;

public class Packet_GameFinished : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_FINISHED;

    public GameResultEnum Result{get; init;}
    public int Player1Score{get; init;}
    public int Player2Score{get; init;}

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
