namespace FourInARowBattle;

public static class ByteBufferExtensions
{
    //NOTE: these functions do not do bound checking

    public static void StoreBigEndianU8(this byte[] array, byte x, int index = 0)
    {
        array[index] = (byte)((x >> 0) & 0xFF);
    }

    public static byte LoadBigEndianU8(this byte[] array, int index = 0)
    {
        byte x = 0;
        for(int i = 0; i < 1; ++i)
        {
            x <<= 8;
            x += array[index + i];
        }
        return x;
    }

    public static void StoreBigEndianU16(this byte[] array, ushort x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 8) & 0xFF);
        array[index + 1] = (byte)((x >> 0) & 0xFF);
    }

    public static ushort LoadBigEndianU16(this byte[] array, int index = 0)
    {
        ushort x = 0;
        x <<= 8; x += array[index + 0];
        x <<= 8; x += array[index + 1];
        return x;
    }

    public static void StoreBigEndianU32(this byte[] array, uint x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 24) & 0xFF);
        array[index + 1] = (byte)((x >> 16) & 0xFF);
        array[index + 2] = (byte)((x >> 08) & 0xFF);
        array[index + 3] = (byte)((x >> 00) & 0xFF);
    }

    public static uint LoadBigEndianU32(this byte[] array, int index = 0)
    {
        uint x = 0;
        x <<= 8; x += array[index + 0];
        x <<= 8; x += array[index + 1];
        x <<= 8; x += array[index + 2];
        x <<= 8; x += array[index + 3];
        return x;
    }

    public static void StoreBigEndianU64(this byte[] array, ulong x, int index = 0)
    {
        array[index + 0] = (byte)((x >> 56) & 0xFF);
        array[index + 1] = (byte)((x >> 48) & 0xFF);
        array[index + 2] = (byte)((x >> 40) & 0xFF);
        array[index + 3] = (byte)((x >> 32) & 0xFF);
        array[index + 4] = (byte)((x >> 24) & 0xFF);
        array[index + 5] = (byte)((x >> 16) & 0xFF);
        array[index + 6] = (byte)((x >> 08) & 0xFF);
        array[index + 7] = (byte)((x >> 00) & 0xFF);
    }

    public static ulong LoadBigEndianU64(this byte[] array, int index = 0)
    {
        ulong x = 0;
        x <<= 8; x += array[index + 0];
        x <<= 8; x += array[index + 1];
        x <<= 8; x += array[index + 2];
        x <<= 8; x += array[index + 3];
        x <<= 8; x += array[index + 4];
        x <<= 8; x += array[index + 5];
        x <<= 8; x += array[index + 6];
        x <<= 8; x += array[index + 7];
        return x;
    }

    public static void StoreBuffer(this byte[] array, byte[] buff, int index = 0)
    {
        for(int i = 0; i < buff.Length; ++i)
            array[i + index] = buff[i];
    }

    public static byte[] LoadBuffer(this byte[] array, int index, int size = 0)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }
}
