using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

/// <summary>
/// Unused packet. Theoretically used by the server to indicate that the lobby will timeout soon.
/// </summary>
public partial class Packet_LobbyTimeoutWarning : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.LOBBY_TIMEOUT_WARNING;

    /// <summary>
    /// How many seconds remain until the timeout
    /// </summary>
    [Export]
    public int SecondsRemaining{get; private set;}

    public Packet_LobbyTimeoutWarning(int secondsRemaining)
    {
        SecondsRemaining = secondsRemaining;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)SecondsRemaining, index, out _);
        return buffer;
    }
    
    public static bool TryConstructPacket_LobbyTimeoutWarningFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int secondsRemaining = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++i) buffer.PopLeft();
        packet = new Packet_LobbyTimeoutWarning(secondsRemaining);
        return true;
    }
}
