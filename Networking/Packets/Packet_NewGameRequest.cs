using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the client to challenge a player to a game
/// </summary>
public partial class Packet_NewGameRequest : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_REQUEST;

    /// <summary>
    /// The index of the player to challenge
    /// </summary>
    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameRequest(int requestTargetIndex)
    {
        RequestTargetIndex = requestTargetIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)RequestTargetIndex, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_NewGameRequestFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameRequest(targetIndex);
        return true;
    }
}
