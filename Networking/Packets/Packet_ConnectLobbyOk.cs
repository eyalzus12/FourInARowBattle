using System;
using System.Collections.Generic;
using System.Linq;
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
}
