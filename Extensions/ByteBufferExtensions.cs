using System;
using System.Numerics;

namespace FourInARowBattle;

/// <summary>
/// byte array extensions for storing data
/// </summary>
public static class ByteBufferExtensions
{
    //NOTE: these functions do not do bound or null checking

    /// <summary>
    /// Store big endian number
    /// </summary>
    /// <param name="array">The array to store in</param>
    /// <param name="x">The number to store</param>
    /// <param name="index">The index to start storing from</param>
    /// <param name="newIndex">The index after the end of the writing</param>
    /// <typeparam name="T">The type of number</typeparam>
    public static void WriteBigEndian<T>(this byte[] array, T x, int index, out int newIndex) where T : IBinaryInteger<T>
    {
        newIndex = index + x.WriteBigEndian(array, index);
    }

    /// <summary>
    /// Store little endian number
    /// </summary>
    /// <param name="array">The array to store in</param>
    /// <param name="x">The number to store</param>
    /// <param name="index">The index to start storing from</param>
    /// <param name="newIndex">The index after the end of the writing</param>
    /// <typeparam name="T">The type of number</typeparam>
    public static void WriteLittleEndian<T>(this byte[] array, T x, int index, out int newIndex) where T : IBinaryInteger<T>
    {
        newIndex = index + x.WriteLittleEndian(array, index);
    }

    /// <summary>
    /// Read big endian number
    /// </summary>
    /// <param name="array">The array to read from</param>
    /// <param name="index">The index to read from</param>
    /// <typeparam name="T">The type of number to read</typeparam>
    /// <returns>The read number</returns>
    public static T ReadBigEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadBigEndian(array, index, true);
    }

    /// <summary>
    /// Read big little number
    /// </summary>
    /// <param name="array">The array to read from</param>
    /// <param name="index">The index to start reading from</param>
    /// <typeparam name="T">The type of number to read</typeparam>
    /// <returns>The read number</returns>
    public static T ReadLittleEndian<T>(this byte[] array, int index = 0) where T : IBinaryInteger<T>
    {
        return T.ReadLittleEndian(array, index, true);
    }

    /// <summary>
    /// Store a byte array inside another byte array
    /// </summary>
    /// <param name="array">The array to store in</param>
    /// <param name="buff">The array to store</param>
    /// <param name="index">The index to start from</param>
    /// <param name="newIndex">The next index after the store</param>
    public static void WriteBuffer(this byte[] array, byte[] buff, int index, out int newIndex)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
        newIndex = index + buff.Length;
    }

    /// <summary>
    /// Read a byte array from another byte array
    /// </summary>
    /// <param name="array">The array to read from</param>
    /// <param name="size">How many bytes to read</param>
    /// <param name="index">Where to start reading from</param>
    /// <returns>The read byte array</returns>
    public static byte[] ReadBuffer(this byte[] array, int size, int index = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }

    /// <summary>
    /// Store a bit array in a packed format inside a byte array
    /// </summary>
    /// <param name="array">The array to store in</param>
    /// <param name="bits">The bit array to store</param>
    /// <param name="index">The index to start storing from</param>
    /// <param name="newIndex">The next index after the store</param>
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

    /// <summary>
    /// Read a packed bit array from a byte array
    /// </summary>
    /// <param name="array">The array to read from</param>
    /// <param name="size">How many bits to read</param>
    /// <param name="index">Where to start reading from</param>
    /// <returns>The read bit array</returns>
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
