using System;
using System.Numerics;

namespace FourInARowBattle;

public static class ByteBufferExtensions
{
    //NOTE: these functions do not do bound or null checking

    public static int WriteBigEndian<T>(this byte[] array, T x, int index = 0) where T : IBinaryInteger<T>
    {
        return x.WriteBigEndian(array, index);
    }

    public static int WriteLittleEndian<T>(this byte[] array, T x, int index = 0) where T : IBinaryInteger<T>
    {
        return x.WriteLittleEndian(array, index);
    }

    public static T ReadBigEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadBigEndian(array, index, true);
    }

    public static T ReadLittleEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadLittleEndian(array, index, true);
    }

    public static int StoreBuffer(this byte[] array, byte[] buff, int index = 0)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
        return index + buff.Length;
    }

    public static byte[] LoadBuffer(this byte[] array, int size, int index = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }
}
