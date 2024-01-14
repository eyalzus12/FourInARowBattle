using System.Diagnostics.CodeAnalysis;
using DequeNet;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A packet used by the server to indicate that a player is no longer in the middle of a game.
/// </summary>
public partial class Packet_LobbyPlayerBusyFalse : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_PLAYER_BUSY_FALSE;

    /// <summary>
    /// The index of the player inside the lobby
    /// </summary>
    [Export]
    public int PlayerIndex{get; private set;}

    public Packet_LobbyPlayerBusyFalse(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)PlayerIndex, index, out _);
        return buffer;
    }
    
    public static bool TryConstructPacket_LobbyPlayerBusyFalseFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int playerIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_LobbyPlayerBusyFalse(playerIndex);
        return true;
    }
}