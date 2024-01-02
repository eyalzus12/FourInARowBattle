using System;

namespace FourInARowBattle;

public static class ByteBufferExtensions
{
    //NOTE: these functions do not do bound checking

    public static void StoreBigEndianU8(this byte[] array, byte x, int index = 0)
    {
        array[index] = (byte)((x >> 0x00) & 0xFF);
    }

    public static void StoreLittleEndianU8(this byte[] array, byte x, int index = 0)
    {
        array[index] = (byte)((x >> 0x00) & 0xFF);
    }

    public static byte LoadBigEndianU8(this byte[] array, int index = 0) =>
        (byte)
        (
            array[index + 0] << 0x00
        );

    public static byte LoadLittleEndianU8(this byte[] array, int index = 0) =>
        (byte)
        (
            array[index + 0] << 0x00
        );

    public static void StoreBigEndianU16(this byte[] array, ushort x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x08) & 0xFF);
        array[index + 1] = (byte)((x >> 0x00) & 0xFF);
    }

    public static void StoreLittleEndianU16(this byte[] array, ushort x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x00) & 0xFF);
        array[index + 1] = (byte)((x >> 0x08) & 0xFF);
    }

    public static ushort LoadBigEndianU16(this byte[] array, int index = 0) =>
        (ushort)
        (
            (array[index + 0] << 0x08) +
            (array[index + 1] << 0x00)
        );

    public static ushort LoadLittleEndianU16(this byte[] array, int index = 0) =>
        (ushort)
        (
            (array[index + 0] << 0x00) +
            (array[index + 1] << 0x08)
        );

    public static void StoreBigEndianU32(this byte[] array, uint x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x18) & 0xFF);
        array[index + 1] = (byte)((x >> 0x10) & 0xFF);
        array[index + 2] = (byte)((x >> 0x08) & 0xFF);
        array[index + 3] = (byte)((x >> 0x00) & 0xFF);
    }

    public static void StoreLittleEndianU32(this byte[] array, uint x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x00) & 0xFF);
        array[index + 1] = (byte)((x >> 0x08) & 0xFF);
        array[index + 2] = (byte)((x >> 0x10) & 0xFF);
        array[index + 3] = (byte)((x >> 0x18) & 0xFF);
    }

    public static uint LoadBigEndianU32(this byte[] array, int index = 0) =>
        (uint)
        (
            (array[index + 0] << 0x18) +
            (array[index + 1] << 0x10) +
            (array[index + 2] << 0x08) +
            (array[index + 3] << 0x00)
        );
    
    public static uint LoadLittleEndianU32(this byte[] array, int index = 0) =>
        (uint)
        (
            (array[index + 0] << 0x00) +
            (array[index + 1] << 0x08) +
            (array[index + 2] << 0x10) +
            (array[index + 3] << 0x18)
        );

    public static void StoreBigEndianU64(this byte[] array, ulong x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x38) & 0xFF);
        array[index + 1] = (byte)((x >> 0x30) & 0xFF);
        array[index + 2] = (byte)((x >> 0x28) & 0xFF);
        array[index + 3] = (byte)((x >> 0x20) & 0xFF);
        array[index + 4] = (byte)((x >> 0x18) & 0xFF);
        array[index + 5] = (byte)((x >> 0x10) & 0xFF);
        array[index + 6] = (byte)((x >> 0x08) & 0xFF);
        array[index + 7] = (byte)((x >> 0x00) & 0xFF);
    }

    public static void StoreLittleEndianU64(this byte[] array, ulong x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 0x00) & 0xFF);
        array[index + 1] = (byte)((x >> 0x08) & 0xFF);
        array[index + 2] = (byte)((x >> 0x10) & 0xFF);
        array[index + 3] = (byte)((x >> 0x18) & 0xFF);
        array[index + 4] = (byte)((x >> 0x20) & 0xFF);
        array[index + 5] = (byte)((x >> 0x28) & 0xFF);
        array[index + 6] = (byte)((x >> 0x30) & 0xFF);
        array[index + 7] = (byte)((x >> 0x38) & 0xFF);
    }

    public static ulong LoadBigEndianU64(this byte[] array, int index = 0) =>
        ((ulong)array[index + 0] << 0x38) +
        ((ulong)array[index + 1] << 0x30) +
        ((ulong)array[index + 2] << 0x28) +
        ((ulong)array[index + 3] << 0x20) +
        ((ulong)array[index + 4] << 0x18) +
        ((ulong)array[index + 5] << 0x10) +
        ((ulong)array[index + 6] << 0x08) +
        ((ulong)array[index + 7] << 0x00);
    
    public static ulong LoadLittleEndianU64(this byte[] array, int index = 0) =>
        ((ulong)array[index + 0] << 0x00) +
        ((ulong)array[index + 1] << 0x08) +
        ((ulong)array[index + 2] << 0x10) +
        ((ulong)array[index + 3] << 0x18) +
        ((ulong)array[index + 4] << 0x20) +
        ((ulong)array[index + 5] << 0x28) +
        ((ulong)array[index + 6] << 0x30) +
        ((ulong)array[index + 7] << 0x38);

    public static void StoreBuffer(this byte[] array, byte[] buff, int index = 0)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
    }

    public static byte[] LoadBuffer(this byte[] array, int size, int index = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }
}
