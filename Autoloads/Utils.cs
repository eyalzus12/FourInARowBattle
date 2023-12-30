namespace FourInARowBattle;

public static class Utils
{
    //NOTE: these functions do not do bound checking

    public static void StoreBigEndianU8(byte x, byte[] array, int index)
    {
        array[index] = (byte)((x >> 0) & 0xFF);
    }

    public static byte LoadBigEndianU8(byte[] array, int index)
    {
        byte x = 0;
        for(int i = 0; i < 1; ++i)
        {
            x <<= 8;
            x += array[index + i];
        }
        return x;
    }

    public static void StoreBigEndianU16(ushort x, byte[] array, int index)
    {
        array[index + 0] = (byte)((x >> 8) & 0xFF);
        array[index + 1] = (byte)((x >> 0) & 0xFF);
    }

    public static ushort LoadBigEndianU16(byte[] array, int index)
    {
        ushort x = 0;
        for(int i = 0; i < 2; ++i)
        {
            x <<= 8;
            x += array[index + i];
        }
        return x;
    }

    public static void StoreBigEndianU32(uint x, byte[] array, int index)
    {
        array[index + 0] = (byte)((x >> 24) & 0xFF);
        array[index + 1] = (byte)((x >> 16) & 0xFF);
        array[index + 2] = (byte)((x >> 08) & 0xFF);
        array[index + 3] = (byte)((x >> 00) & 0xFF);
    }

    public static uint LoadBigEndianU32(byte[] array, int index)
    {
        uint x = 0;
        for(int i = 0; i < 4; ++i)
        {
            x <<= 8;
            x += array[index + i];
        }
        return x;
    }

    public static void StoreBigEndianU64(ulong x, byte[] array, int index)
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

    public static ulong LoadBigEndianU64(byte[] array, int index)
    {
        ulong x = 0;
        for(int i = 0; i < 8; ++i)
        {
            x <<= 8;
            x += array[index + i];
        }
        return x;
    }

    public static int StoreBuffer(byte[] buff, byte[] array, int index)
    {
        for(int i = 0; i < buff.Length; ++i)
        {
            array[i + index] = buff[i];
        }
        return index + buff.Length;
    }

    public static byte[] LoadBuffer(byte[] array, int index, int size)
    {
        byte[] buff = new byte[size];
        for(int i = 0; i < size; ++i)
            buff[i] = array[i + index];
        return buff;
    }
}
