using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate the a player accepted a game request
/// </summary>
public partial class Packet_NewGameAccepted : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPTED;

    /// <summary>
    /// The index of the accepting player
    /// </summary>
    [Export]
    public int RequestTargetIndex{get; private set;}

    public Packet_NewGameAccepted(int requestTargetIndex)
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

    public static bool TryConstructPacket_NewGameAcceptedFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int targetIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAccepted(targetIndex);
        return true;
    }
}
