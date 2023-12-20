using Godot;
using System;

namespace FourInARowBattle;

public partial class Packet : RefCounted
{
    public enum MessageTypeEnum : ushort
    {
        //test packet
        DUMMY = 0
    }

    [Export]
    public MessageTypeEnum Type{get; set;} = MessageTypeEnum.DUMMY;
    [Export]
    public byte[] Bytes{get; set;} = Array.Empty<byte>();

    public byte[] ToByteArray()
    {
        byte[] result = new byte[2 + 4 + Bytes.Length];

        uint packetSize = (uint)Bytes.Length;

        ushort rawType = (ushort)Type;

        int p = 0;
        p = Utils.StoreBigEndian(rawType, result, p);
        p = Utils.StoreBigEndian(packetSize, result, p);
        for(uint i = 0; i < packetSize; ++i)
            result[p++] = Bytes[i];
        return result;
    }
}
