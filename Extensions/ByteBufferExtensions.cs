using System;
using System.Numerics;

namespace FourInARowBattle;

public static class ByteBufferExtensions
{
    //NOTE: these functions do not do bound or null checking

    public static void WriteBigEndian<T>(this byte[] array, T x, int index, out int newIndex) where T : IBinaryInteger<T>
    {
        newIndex = index + x.WriteBigEndian(array, index);
    }

    public static void WriteLittleEndian<T>(this byte[] array, T x, int index, out int newIndex) where T : IBinaryInteger<T>
    {
        newIndex = index + x.WriteLittleEndian(array, index);
    }

    public static T ReadBigEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadBigEndian(array, index, true);
    }

    public static T ReadLittleEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadLittleEndian(array, index, true);
    }

    public static void WriteBuffer(this byte[] array, byte[] buff, int index, out int newIndex)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
        newIndex = index + buff.Length;
    }

    public static byte[] ReadBuffer(this byte[] array, int size, int index = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }

    public static void WriteBits(this byte[] array, bool[] bits, int index, out int newIndex)
    {
        byte[] packed = new byte[(int)Math.Ceiling(bits.Length / 8.0)];
        for(int b = 0; b < bits.Length; ++b)
        {
            if(bits[b])
            {
                int i = b / 8, j = b % 8;
                packed[i] |= (byte)(1 << j);
            }
        }
        WriteBuffer(array, packed, index, out newIndex);
    }

    public static bool[] ReadBits(this byte[] array, int size, int index = 0)
    {
        byte[] packed = array.ReadBuffer((int)Math.Ceiling(size / 8.0), index);
        bool[] bits = new bool[size];
        for(int b = 0; b < bits.Length; ++b)
        {
            int i = b / 8, j = b % 8;
            bits[b] = (packed[i] & (1 << j)) != 0;
        }
        return bits;
    }
}
