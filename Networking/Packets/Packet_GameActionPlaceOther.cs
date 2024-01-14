using Godot;
using System.Diagnostics.CodeAnalysis;
using DequeNet;

namespace FourInARowBattle;

public partial class Packet_GameActionPlaceOther : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.GAME_ACTION_PLACE_OTHER;

    [Export]
    public byte Column{get; private set;}
    [Export]
    public string ScenePath{get; private set;} = null!;

    public Packet_GameActionPlaceOther(byte column, string scenePath)
    {
        Column = column;
        ScenePath = scenePath;
    }

    public override byte[] ToByteArray()
    {
        byte[] stringBuffer = ScenePath.ToUtf8Buffer();
        byte[] buffer = new byte[sizeof(byte) + sizeof(byte) + sizeof(uint) + stringBuffer.Length];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian(Column, index, out index);
        buffer.WriteBigEndian((uint)stringBuffer.Length, index, out index);
        buffer.WriteBuffer(stringBuffer, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_GameActionPlaceOtherFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 6) return false;
        uint size = new[]{buffer[2], buffer[3], buffer[4], buffer[5]}.ReadBigEndian<uint>();
        if(buffer.Count < 6 + size) return false;
        byte column = buffer[1];
        for(int i = 0; i < 6; ++i) buffer.PopLeft();
        byte[] path = new byte[size]; for(int i = 0; i < size; ++i) path[i] = buffer.PopLeft();
        packet = new Packet_GameActionPlaceOther(column, path.GetStringFromUtf8());
        return true;
    }
}
