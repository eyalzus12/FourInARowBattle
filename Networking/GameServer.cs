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
        public GameTurnEnum[]? Turns{get; set;} = null;

        public Player? Requester{get; set;} = null;
        public Game? ActiveGame{get; set;} = null;
    }

    [Export]
    public WebSocketServer? Server{get; set;}
    [Export]
    public PackedScene? GameScene{get; set;} = null;

    private readonly Deque<byte> _buffer = new();

    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<uint, Lobby> _lobbies = new();

    public override void _Ready()
    {
        Autoloads.PersistentData.HeadlessMode = true;

        if(GameScene is null)
        {
            GD.PushError("Attempt to start server with a null game scene");
        }

        if(Server is not null)
        {
            Server.PacketReceived += OnWebSocketServerPacketReceived;
            Server.ClientConnected += OnWebSocketClientConnected;
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

    public void OnWebSocketClientConnected(int peerId)
    {
        GD.Print($"New client {peerId}");
    }

    public void OnWebSocketClientDisconnected(int peerId)
    {
        GD.Print($"Client {peerId} disconnected");
        RemovePlayer(peerId, DisconnectReasonEnum.CONNECTION);
    }

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
                    GD.Print($"{peerId} failed to create lobby. They are already in one.");
                    SendPacket(peerId, new Packet_CreateLobbyFail(ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY));
                    break;
                }

                //create lobby
                uint id; do{id = GD.Randi();} while(!_lobbies.ContainsKey(id));
                Lobby lobby = new(){Id = id};
                lobby.Players[0] = player;
                player.Lobby = lobby;
                _lobbies[id] = lobby;
                GD.Print($"Created lobby {id} for {peerId}");

                //respond
                SendPacket(peerId, new Packet_CreateLobbyOk(id));
                break;
            }
            case Packet_ConnectLobbyRequest _packet:
            {
                GD.Print($"{peerId} wants to connect to lobby {_packet.LobbyId} with name {_packet.PlayerName}");

                if(!UpdateName(peerId, _packet.PlayerName, out Player? player))
                {
                    GD.Print($"{peerId} failed to connect to lobby. They are already in one.");
                    SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_WHILE_IN_LOBBY));
                    break;
                }
                if(!_lobbies.TryGetValue(_packet.LobbyId, out Lobby? lobby))
                {
                    GD.Print($"{peerId} failed to connect to lobby {_packet.LobbyId}. That lobby does not exist.");
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
                    GD.Print($"{peerId} failed to connect to lobby {_packet.LobbyId}. That lobby is full.");
                    SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_LOBBY_FULL));
                    break;
                }

                GD.Print($"{peerId} connected to lobby {_packet.LobbyId}");

                SendPacket(player.Id, new Packet_ConnectLobbyOk(other?.Name ?? ""));
                if(other is not null)
                    SendPacket(other.Id, new Packet_LobbyNewPlayer(player.Name!));
                break;
            }
            case Packet_NewGameRequest:
            {
                GD.Print($"{peerId} try request new game");

                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    GD.Print($"{peerId} cannot request game start because they are not in a lobby.");
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Players[1] is null)
                {
                    GD.Print($"{peerId} cannot request game start because there is no other player.");
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_OTHER_PLAYER));
                    break;
                }
                if(lobby.Requester is not null)
                {
                    GD.Print($"{peerId} cannot request game start because they already did.");
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID));
                    break;
                }
                if(lobby.ActiveGame is not null)
                {
                    GD.Print($"{peerId} cannot request game start because they are in the middle of a game.");
                    SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME));
                    break;
                }

                GD.Print($"{peerId} requested game start");
                lobby.Requester = player;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameRequestOk());
                SendPacket(other.Id, new Packet_NewGameRequested());
                break;
            }
            case Packet_NewGameAccept:
            {
                GD.Print($"{peerId} try approve new game request");

                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    GD.Print($"{peerId} cannot approve game request because they are not in a lobby.");
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    GD.Print($"{peerId} cannot approve game request because there is no request");
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST));
                    break;
                }
                if(lobby.Requester == player)
                {
                    GD.Print($"{peerId} cannot approve their own request");
                    SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_YOUR_REQUEST));
                    break;
                }

                GD.Print($"{peerId} approved game request");

                lobby.Requester = null;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameAcceptOk());
                SendPacket(other.Id, new Packet_NewGameAccepted());
                /*
                start game here
                */
                GD.Print($"game will now started in lobby {lobby.Id}");
                lobby.ActiveGame = Autoloads.ScenePool.GetScene<Game>(GameScene!);
                AddChild(lobby.ActiveGame);
                bool which = GD.RandRange(0, 1) == 0; //decide which player is first
                lobby.Turns = new GameTurnEnum[]{which ? GameTurnEnum.Player1 : GameTurnEnum.Player2, which ? GameTurnEnum.Player2 : GameTurnEnum.Player1};
                SendPacket(player.Id, new Packet_NewGameStarting(lobby.Turns[lobby.Players[0] == player ? 0 : 1]));
                SendPacket(other.Id, new Packet_NewGameStarting(lobby.Turns[lobby.Players[0] == other ? 0 : 1]));
                break;
            }
            case Packet_NewGameReject:
            {
                GD.Print($"{peerId} try reject new game request");
                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    GD.Print($"{peerId} cannot reject game request because they are not in a lobby");
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    GD.Print($"{peerId} cannot reject game request because there is no request");
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST));
                    break;
                }
                if(lobby.Requester == player)
                {
                    GD.Print($"{peerId} cannot reject their own request");
                    SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_YOUR_REQUEST));
                    break;
                }

                GD.Print($"{peerId} rejected game request");
                lobby.Requester = null;
                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
                SendPacket(player.Id, new Packet_NewGameRejectOk());
                SendPacket(other.Id, new Packet_NewGameRejected());
                break;
            }
            case Packet_NewGameCancel:
            {
                GD.Print($"{peerId} try cancel new game request");
                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    GD.Print($"{peerId} cannot cancel game request because they are not in a lobby");
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY));
                    break;
                }
                Lobby lobby = player.Lobby;

                if(lobby.Requester is null)
                {
                    GD.Print($"{peerId} cannot cancel game request because there is no request");
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST));
                    break;
                }
                if(lobby.Requester != player)
                {
                    GD.Print($"{peerId} cannot cancel the other player's request");
                    SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_YOUR_REQUEST));
                    break;
                }

                GD.Print($"{peerId} canceled game request");

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
                GD.Print($"{peerId} try place \"{_packet.ScenePath}\" at column {_packet.Column}");
                if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
                {
                    GD.Print($"{peerId} failed to place token because they are not in a game");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME));
                    break;
                }
                Lobby lobby = player.Lobby;
                if(lobby.ActiveGame is null)
                {
                    GD.Print($"{peerId} failed to place token because they are not in a game");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME));
                    break;
                }
                Game game = lobby.ActiveGame;
                GameTurnEnum playerTurn = (lobby.Players[0] == player)?lobby.Turns![0]:lobby.Turns![1];
                if(game.Turn != playerTurn)
                {
                    GD.Print($"{peerId} failed to place token because it is not their turn");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_YOUR_TURN));
                    break;
                }
                int column = _packet.Column;

                if(column < 0 || game.GameBoard.Columns <= column)
                {
                    GD.Print($"{peerId} failed to place token because the column was invalid");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_COLUMN));
                    break;
                }

                string scenePath = _packet.ScenePath;
                //not such path
                if(!ResourceLoader.Exists(scenePath))
                {
                    GD.Print($"{peerId} failed to place token because the scene path does not exist");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
                    break;
                }
                //load scene
                Resource res = ResourceLoader.Load(scenePath);
                //path does not point to a scene
                if(res is not PackedScene scene)
                {
                    GD.Print($"{peerId} failed to place token because the scene path does not point to a scene");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
                    break;
                }
                TokenBase? token = Autoloads.ScenePool.GetSceneOrNull<TokenBase>(scene);
                //scene is not a token
                if(token is null)
                {
                    GD.Print($"{peerId} failed to place token because the scene is not a token");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
                    break;
                }
                //find matching token counter
                TokenCounterControl? control = null;
                foreach(TokenCounterListControl lc in game.CounterLists)
                {
                    foreach(TokenCounterControl c in lc.Counters)
                    {
                        foreach(TokenCounterButton b in c.TokenButtons)
                        {
                            if(b.AssociatedScene == scene)
                            {
                                control = c;
                                break;
                            }
                        }
                        if(control is not null) break;
                    }
                    if(control is not null) break;
                }
                //attempt to use unusable token
                if(control is null)
                {
                    GD.Print($"{peerId} failed to place token because the token is invalid");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
                    break;
                }
                //not enough tokens to use
                if(!control.CanTake())
                {
                    GD.Print($"{peerId} failed to place token because they don't have enough tokens");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS));
                    break;
                }

                token.TokenColor = game.TurnColor;
                if(game.GameBoard.AddToken(column, token))
                {
                    control.Take(1);
                    game.PassTurn();
                }
                else
                {
                    GD.Print($"{peerId} failed to place token because the column is full");
                    SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN));
                    break;
                }

                GD.Print($"{peerId} placed token");

                Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;

                SendPacket(player.Id, new Packet_GameActionPlaceOk());
                SendPacket(other.Id, new Packet_GameActionPlaceOther((byte)column, scenePath));
                break;
            }
            case Packet_GameActionRefill:
            {
                GD.Print($"{peerId} try refill");
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
        if(lobby.ActiveGame is not null)
            Autoloads.ScenePool.ReturnScene(lobby.ActiveGame);
        lobby.ActiveGame = null;
        lobby.Turns = null;
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