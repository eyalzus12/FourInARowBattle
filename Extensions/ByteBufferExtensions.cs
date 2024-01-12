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

    public static void StoreBuffer(this byte[] array, byte[] buff, int index, out int newIndex)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
        newIndex = index + buff.Length;
    }

    public static byte[] LoadBuffer(this byte[] array, int size, int index = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }
}
