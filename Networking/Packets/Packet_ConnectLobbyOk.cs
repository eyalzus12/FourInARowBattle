using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class Packet_ConnectLobbyOk : AbstractPacket
{
    public override PacketTypeEnum PacketType => PacketTypeEnum.CONNECT_LOBBY_OK;

    [Export]
    public int YourIndex{get; set;}
    [Export]
    public Godot.Collections.Array<string> Players{get; set;} = new();

    public Packet_ConnectLobbyOk(int yourIndex, string[] players)
    {
        YourIndex = yourIndex;
        Players = players.ToGodotArray();
    }

    public override byte[] ToByteArray()
    {
        int bufferSize = sizeof(byte) + sizeof(uint) + sizeof(uint);
        byte[][] playerNameBuffers = new byte[Players.Count][];
        for(int i = 0; i < Players.Count; ++i)
        {
            string playerName = Players[i];
            byte[] nameBuffer = playerName.ToUtf8Buffer();
            if(nameBuffer.Length > Globals.NAME_LENGTH_LIMIT)
            {
                GD.PushError($"Player name has invalid length {nameBuffer.Length}");
                nameBuffer = nameBuffer.Take(Globals.NAME_LENGTH_LIMIT).ToArray();
            }
            playerNameBuffers[i] = nameBuffer;
            bufferSize += sizeof(byte) + nameBuffer.Length;
        }
        byte[] buffer = new byte[bufferSize];
        buffer.WriteBigEndian((byte)PacketType, 0, out int index);
        buffer.WriteBigEndian((uint)YourIndex, index, out index);
        buffer.WriteBigEndian((uint)Players.Count, index, out index);
        for(int i = 0; i < Players.Count; ++i)
        {
            buffer.WriteBigEndian((byte)playerNameBuffers[i].Length, index, out index);
            buffer.StoreBuffer(playerNameBuffers[i], index, out index);
        }
        return buffer;
    }
}
