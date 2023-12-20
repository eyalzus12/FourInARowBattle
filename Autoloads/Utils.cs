namespace FourInARowBattle;

public static class Utils
{
    public static int StoreBigEndian(ushort x, byte[] array, int index)
    {
        array[index++] = (byte)((x >> 8) & 0xFF);
        array[index++] = (byte)((x >> 0) & 0xFF);
        return index;
    }

    public static int StoreBigEndian(uint x, byte[] array, int index)
    {
        array[index++] = (byte)((x >> 24) & 0xFF);
        array[index++] = (byte)((x >> 16) & 0xFF);
        array[index++] = (byte)((x >> 08) & 0xFF);
        array[index++] = (byte)((x >> 00) & 0xFF);
        return index;
    }
}
