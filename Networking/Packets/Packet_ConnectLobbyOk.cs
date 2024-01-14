using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DequeNet;
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_OK;

    [Export]
    public int YourIndex{get; private set;}
    [Export]
    public LobbyPlayerData[] Players{get; private set;} = Array.Empty<LobbyPlayerData>();

    public Packet_ConnectLobbyOk(int yourIndex, LobbyPlayerData[] players)
    {
        YourIndex = yourIndex;
        Players = players;
    }

    public override byte[] ToByteArray()
    {
        int bufferSize = sizeof(byte) + sizeof(uint) + sizeof(uint);
        byte[][] playerNameBuffers = new byte[Players.Length][];
        for(int i = 0; i < Players.Length; ++i)
        {
            string playerName = Players[i].Name;
            if(playerName.Length > Globals.NAME_LENGTH_LIMIT)
            {
                GD.Print($"Player name has invalid length {playerName.Length}");
                playerName = new(playerName.Take(Globals.NAME_LENGTH_LIMIT).ToArray());
            }
            byte[] nameBuffer = playerName.ToUtf8Buffer();
            playerNameBuffers[i] = nameBuffer;
            bufferSize += sizeof(byte) + nameBuffer.Length;
        }
        bool[] busys = Players.Select(p => p.Busy).ToArray();
        //bit buffer
        bufferSize += (int)Math.Ceiling(Players.Length / 8.0);
        byte[] buffer = new byte[bufferSize];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)YourIndex, index, out index);
        buffer.WriteBigEndian((uint)Players.Length, index, out index);
        for(int i = 0; i < Players.Length; ++i)
        {
            buffer.WriteBigEndian((byte)playerNameBuffers[i].Length, index, out index);
            buffer.WriteBuffer(playerNameBuffers[i], index, out index);
        }
        buffer.WriteBits(busys, index, out _);
        return buffer;
    }

    public static bool TryConstructPacket_ConnectLobbyOkFrom(Deque<byte> buffer, [NotNullWhen(true)] out AbstractPacket? packet)
    {
        packet = null;
        if(buffer.Count < 9) return false;
        int playerCount = (int)new[]{buffer[5], buffer[6], buffer[7], buffer[8]}.ReadBigEndian<uint>();
        int packedBusyCount = (int)Math.Ceiling(playerCount / 8.0);
        //early "enough space" check
        if(buffer.Count < 9 + playerCount + packedBusyCount) return false;
        int index = 9;
        //ensure enough space for names
        for(int i = 0; i < playerCount; ++i)
        {
            if(index >= buffer.Count) return false;
            byte size = buffer[index];
            index += size + 1;
        }
        //not enough for busy bits
        if(index + packedBusyCount > buffer.Count) return false;

        int yourIndex = (int)new[]{buffer[1], buffer[2], buffer[3], buffer[4]}.ReadBigEndian<uint>();
        byte[][] nameBuffers = new byte[playerCount][];
        string[] names = new string[playerCount];
        byte[] packedBusy = new byte[packedBusyCount];
        for(int i = 0; i < 9; ++i) buffer.PopLeft();
        //read names
        for(int i = 0; i < playerCount; ++i)
        {
            byte size = buffer.PopLeft();
            nameBuffers[i] = new byte[size];
            for(int j = 0; j < size; ++j)
            {
                nameBuffers[i][j] = buffer.PopLeft();
            }
            names[i] = nameBuffers[i].GetStringFromUtf8();
            if(names[i].Length > Globals.NAME_LENGTH_LIMIT)
            {
                GD.Print($"Packet has name with invalid length {names[i].Length}. It will be trimmed.");
                names[i] = new(names[i].Take(Globals.NAME_LENGTH_LIMIT).ToArray());
            }
        }
        //read busy bits
        for(int i = 0; i < packedBusyCount; ++i) packedBusy[i] = buffer.PopLeft();
        bool[] busyBits = packedBusy.ReadBits(playerCount, 0);
        //convert into player list
        LobbyPlayerData[] players = names
            .Zip(busyBits, (string name, bool busy) => new LobbyPlayerData(name, busy))
            .ToArray();
        packet = new Packet_ConnectLobbyOk(yourIndex, players);
        return true;
    }
}
