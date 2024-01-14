using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_NewGameAccept : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.NEW_GAME_ACCEPT;

    [Export]
    public int RequestSourceIndex{get; private set;}

    public Packet_NewGameAccept(int requestSourceIndex)
    {
        RequestSourceIndex = requestSourceIndex;
    }

    public override byte[] ToByteArray()
    {
        byte[] buffer = new byte[sizeof(byte) + sizeof(uint)];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)RequestSourceIndex, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_NewGameAcceptFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 5) return false;
        int sourceIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        for(int i = 0; i < 5; ++ i) buffer.PopLeft();
        packet = new Packet_NewGameAccept(sourceIndex);
        return true;
    }
}
