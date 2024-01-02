using Godot;
using DequeNet;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FourInARowBattle;

public partial class GameServer : Node
{
    private sealed class Player
    {
        public Player(int id, string name)
        {
            Id = id;
            Name = name;
            Lobby = null;
        }

        public int Id{get; init;}
        public string Name{get; set;}
        public Lobby? Lobby{get; set;}
    }
    
    private sealed class Lobby
    {
        public uint Id{get; init;}
        public Player?[] Players{get; private set;} = new Player[2];

        public Player? Requester{get; set;} = null; 
        public bool InGame{get; set;} = false;
    }

    [Export]
    public WebSocketServer? Server{get; set;}

    private readonly Deque<byte> _buffer = new();

    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<uint, Lobby> _lobbies = new();

    public override void _Ready()
    {
        if(Server is not null)
        {
            Server.PacketReceived += OnWebSocketServerPacketReceived;
            Server.ClientDisconnected += OnWebSocketClientDisconnected;
        }
    }

    public void OnWebSocketServerPacketReceived(int peerId, byte[] packetBytes)
    {
        _buffer.PushRightRange(packetBytes);
        
        while(_buffer.Count > 0 && AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(peerId, packet);
        }
    }

    public void OnWebSocketClientDisconnected(int peerId) => RemovePlayer(peerId, DisconnectReasonEnum.CONNECTION);

    public void SendPacket(int peerId, AbstractPacket packet)
    {
        Server?.SendPacket(peerId, packet.ToByteArray());
    }

    public void HandlePacket(int peerId, AbstractPacket packet)
    {
        switch(packet)
        {
            case Packet_Dummy:
            {
                GD.Print($"Got dummy packet from {peerId}");
                break;
            }
            case Packet_InvalidPacket _packet:
            {
                GD.Print($"Got invalid packet from {peerId}: {_packet.GivenPacketType}");
                SendPacket(peerId, new Packet_InvalidPacketInform(_packet.GivenPacketType));
                break;
            }
            case Packet_CreateLobbyRequest _packet:
            {
                GD.Print($"{peerId} wants to create lobby. Player name: {_packet.PlayerName}");

                if(!UpdateName(peerId, _packet.PlayerName, out Player? player))
                {
                    SendPacket(peerId, new Packet_CreateLobbyFail(ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY));
                    break;
                }

                //create lobby
                uint id; do{id = GD.Randi();} while(!_lobbies.ContainsKey(id));
                Lobby lobby = new(){Id = id};
                lobby.Players[0] = player;
                player.Lobby = lobby;
                _lobbies[id] = lobby;

                //respond
                SendPacket(peerId, new Packet_CreateLobbyOk(id));
                break;
            }
            case Packet_ConnectLobbyRequest _packet:
            {
                GD.Print($"{peerId} wants to connect to lobby {_packet.LobbyId} with name {_packet.PlayerName}");

                if(!UpdateName(peerId, _packet.PlayerName, out Player? player))
                {
                    SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_WHILE_IN_LOBBY));
                    break;
                }
                if(!_lobbies.TryGetValue(_packet.LobbyId, out Lobby? lobby))
                {
                    SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_LOBBY_DOES_NOT_EXIST));
                    break;
                }

                Player? other = null;
                //first spot avail
                if(lobby.Players[0] is null)
                {
                    lobby.Players[0] = player;
                    player.Lobby = lobby;
                }
                //second spot avail
                else if(lobby.Players[1] is null)
                {
                    lobby.Players[1] = player;
                    player.Lobby = lobby;
                    other = lobby.Players[0];
                }
                //full
                else
                {
                    SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_LOBBY_FULL));
                    break;
                }

                SendPacket(player.Id, new Packet_ConnectLobbyOk(other?.Name ?? ""));
                if(other is not null)
                    SendPacket(other.Id, new Packet_LobbyNewPlayer(player.Name!));
                break;
            }
            case Packet_NewGameRequest:
            {
                GD.Print($"{peerId} wants to start new game");

                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Players[1] is null)
                {
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_OTHER_PLAYER));
                    break;
                }
                if(lobby.Requester is not null)
                {
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID));
                    break;
                }
                if(lobby.InGame)
                {
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME));
                    break;
                }

                lobby.Requester = player;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameRequestOk());
                SendPacket(other.Id, new Packet_NewGameRequested());
                break;
            }
            case Packet_NewGameAccept:
            {
                GD.Print($"{peerId} approves new game request");

                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST));
                    break;
                }
                if(lobby.Requester == player)
                {
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_YOUR_REQUEST));
                    break;
                }

                lobby.Requester = null;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameAcceptOk());
                SendPacket(other.Id, new Packet_NewGameAccepted());
                break;
            }
            case Packet_NewGameReject:
            {
                GD.Print($"{peerId} rejects new game request");
                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST));
                    break;
                }
                if(lobby.Requester == player)
                {
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_YOUR_REQUEST));
                    break;
                }

                lobby.Requester = null;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameRejectOk());
                SendPacket(other.Id, new Packet_NewGameRejected());
                break;
            }
            case Packet_NewGameCancel:
            {
                GD.Print($"{peerId} cancels new game request");
                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST));
                    break;
                }
                if(lobby.Requester != player)
                {
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_YOUR_REQUEST));
                    break;
                }

                lobby.Requester = null;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameCancelOk());
                SendPacket(other.Id, new Packet_NewGameCanceled());
                break;
            }
            case Packet_LobbyDisconnect _packet:
            {
                GD.Print($"{peerId} is disconnecting. Reason: {_packet.Reason}");
                RemovePlayer(peerId, _packet.Reason);
                break;
            }
            case Packet_GameActionPlace _packet:
            {
                GD.Print($"{peerId} is placing {_packet.ScenePath} at {_packet.Column}");
                break;
            }
            case Packet_GameActionRefill:
            {
                GD.Print($"{peerId} is refilling");
                break;
            }
            default:
            {
                GD.PushError($"Server did not expect packet of type {packet.GetType().Name} from {peerId}");
                break;
            }
        }
    }

    private void RemovePlayer(int peerId, DisconnectReasonEnum reason)
    {
        if(!_players.TryGetValue(peerId, out Player? player)) return;
        _players.Remove(player.Id);
        Lobby? lobby = player.Lobby;
        if(lobby is null) return;
        lobby.InGame = false;
        lobby.Requester = null;

        if(lobby.Players[0] == player)
        {
            lobby.Players[0] = lobby.Players[1];
            lobby.Players[1] = null;
        }
        else
        {
            lobby.Players[1] = null;
        }
        Player? other = lobby.Players[0];
        if(other is null) _lobbies.Remove(lobby.Id);
        else SendPacket(other.Id, new Packet_LobbyDisconnectOther(reason));
    }

    private bool UpdateName(int peerId, string name, [NotNullWhen(true)] out Player? player)
    {
        if(_players.TryGetValue(peerId, out player))
        {
            if(player.Lobby is not null)
            {
                return false;
            }
            //update name
            player.Name = name;
        }
        //new player
        else
        {
            player = _players[peerId] = new Player(peerId, name);
        }
        return true;
    }
}