using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

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
    
    public static bool TryConstructPacket_NewGameStartingFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        GameTurnEnum turn = (GameTurnEnum)buffer[1];
        int opponentIndex = (int)new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        for(int i = 0; i < 6; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameStarting(turn, opponentIndex);
        return true;
    }
}
