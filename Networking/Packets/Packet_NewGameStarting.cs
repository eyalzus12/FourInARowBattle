using Godot;

namespace FourInARowBattle;

public partial class Packet_NewGameStarting : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_STARTING;

    [Export]
    public GameTurnEnum GameTurn{get; set;}

    public Packet_NewGameStarting(GameTurnEnum gameTurn)
    {
        GameTurn = gameTurn;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[1 + 1];
        buffer.StoreBigEndianU8((byte)PacketType, 0);
        buffer.StoreBigEndianU8((byte)GameTurn, 0);
        return buffer;
    }
}
