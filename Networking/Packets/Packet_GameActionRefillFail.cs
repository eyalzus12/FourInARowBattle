
namespace FourInARowBattle;

public class Packet_GameActionRefillFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_REFILL_FAIL;

    public ErrorCodeEnum ErrorCode{get; init;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)ErrorCode, buffer, 1);
        return buffer;
    }
}
