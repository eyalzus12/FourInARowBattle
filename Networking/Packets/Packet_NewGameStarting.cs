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
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((byte)GameTurn, index, out _);
        return buffer;
    }
}
