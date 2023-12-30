namespace FourInARowBattle;

public class Packet_NewGameCancelFail : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_CANCEL_FAIL;

    public ErrorCodeEnum ErrorCode{get; init;}

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        Utils.StoreBigEndianU8((byte)PacketType, buffer, 0);
        Utils.StoreBigEndianU8((byte)ErrorCode, buffer, 1);
        return buffer;
    }
}
