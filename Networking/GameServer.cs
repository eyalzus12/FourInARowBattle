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
    public WebSocketServer Server{get; set;} = null!;
    [Export]
    public PackedScene GameScene{get; set;} = null!;
    [Export]
    public bool RefuseNewConnections{get => Server.RefuseNewConnections; set => Server.RefuseNewConnections = value;}

    private readonly Deque<byte> _buffer = new();

    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<uint, Lobby> _lobbies = new();

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Server);
        ArgumentNullException.ThrowIfNull(GameScene);
    }

    private void ConnectSignals()
    {
        Server.PacketReceived += OnWebSocketServerPacketReceived;
        Server.ClientConnected += OnWebSocketClientConnected;
        Server.ClientDisconnected += OnWebSocketClientDisconnected;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    public Error Listen(ushort port)
    {
        Error err = Server.Listen(port);
        if(err != Error.Ok) return err;
        Autoloads.PersistentData.HeadlessMode = true;
        return Error.Ok;
    }

    public void Stop()
    {
        foreach(Lobby lobby in _lobbies.Values)
        {
            lobby.ActiveGame?.QueueFreeDeferred();
        }

        foreach(Player player in _players.Values)
        {
            SendPacket(player.Id, new Packet_ServerClosing());
        }

        Server.Stop();
        Autoloads.PersistentData.HeadlessMode = false;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            Stop();
        }
    }

    private void OnWebSocketServerPacketReceived(int peerId, byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        GD.Print("server got packet");
        _buffer.PushRightRange(packetBytes);

        while(_buffer.Count > 0 && AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(peerId, packet);
        }
    }

    private void OnWebSocketClientConnected(int peerId)
    {
        GD.Print($"New client {peerId}");
        _players[peerId] = new Player(peerId, "");
    }

    private void OnWebSocketClientDisconnected(int peerId)
    {
        GD.Print($"Client {peerId} disconnected");
        RemovePlayer(peerId, DisconnectReasonEnum.CONNECTION);
    }

    #region Packet Handling

    public void SendPacket(int peerId, AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        Server?.SendPacket(peerId, packet.ToByteArray());
    }

    public void HandlePacket(int peerId, AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        switch(packet)
        {
            case Packet_Dummy _packet:
                HandlePacket_Dummy(peerId, _packet);
                break;
            case Packet_InvalidPacket _packet:
                HandlePacket_InvalidPacket(peerId, _packet);
                break;
            case Packet_CreateLobbyRequest _packet:
                HandlePacket_CreateLobbyRequest(peerId, _packet);
                break;
            case Packet_ConnectLobbyRequest _packet:
                HandlePacket_ConnectLobbyRequest(peerId, _packet);
                break;
            case Packet_NewGameRequest _packet:
                HandlePacket_NewGameRequest(peerId, _packet);
                break;
            case Packet_NewGameAccept _packet:
                HandlePacket_NewGameAccept(peerId, _packet);
                break;
            case Packet_NewGameReject _packet:
                HandlePacket_NewGameReject(peerId, _packet);
                break;
            case Packet_NewGameCancel _packet:
                HandlePacket_NewGameCancel(peerId, _packet);
                break;
            case Packet_LobbyDisconnect _packet:
                HandlePacket_LobbyDisconnect(peerId, _packet);
                break;
            case Packet_GameActionPlace _packet:
                HandlePacket_GameActionPlace(peerId, _packet);
                break;
            case Packet_GameActionRefill _packet:
                HandlePacket_GameActionRefill(peerId, _packet);
                break;
            default:
            {
                GD.PushError($"Server did not expect packet of type {packet.GetType().Name} from {peerId}");
                break;
            }
        }
    }

    private void HandlePacket_Dummy(int peerId, Packet_Dummy packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got dummy packet from {peerId}");
    }

    private void HandlePacket_InvalidPacket(int peerId, Packet_InvalidPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got invalid packet from {peerId}: {packet.GivenPacketType}");
        SendPacket(peerId, new Packet_InvalidPacketInform(packet.GivenPacketType));
    }

    private void HandlePacket_CreateLobbyRequest(int peerId, Packet_CreateLobbyRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);
        GD.Print($"{peerId} wants to create lobby. Player name: {packet.PlayerName}");

        if(!UpdateName(peerId, packet.PlayerName, out Player? player))
        {
            GD.Print($"{peerId} failed to create lobby. They are already in one.");
            SendPacket(peerId, new Packet_CreateLobbyFail(ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY));
            return;
        }

        //create lobby
        uint id; do{id = GD.Randi();} while(_lobbies.ContainsKey(id));
        Lobby lobby = new(){Id = id};
        lobby.Players[0] = player;
        player.Lobby = lobby;
        _lobbies[id] = lobby;
        GD.Print($"Created lobby {id} for {peerId}");

        //respond
        SendPacket(peerId, new Packet_CreateLobbyOk(id));
    }

    private void HandlePacket_ConnectLobbyRequest(int peerId, Packet_ConnectLobbyRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);
        GD.Print($"{peerId} wants to connect to lobby {packet.LobbyId} with name {packet.PlayerName}");

        if(!UpdateName(peerId, packet.PlayerName, out Player? player))
        {
            GD.Print($"{peerId} failed to connect to lobby. They are already in one.");
            SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_WHILE_IN_LOBBY));
            return;
        }
        if(!_lobbies.TryGetValue(packet.LobbyId, out Lobby? lobby))
        {
            GD.Print($"{peerId} failed to connect to lobby {packet.LobbyId}. That lobby does not exist.");
            SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_LOBBY_DOES_NOT_EXIST));
            return;
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
            GD.Print($"{peerId} failed to connect to lobby {packet.LobbyId}. That lobby is full.");
            SendPacket(peerId, new Packet_ConnectLobbyFail(ErrorCodeEnum.CANNOT_JOIN_LOBBY_FULL));
            return;
        }

        GD.Print($"{peerId} connected to lobby {packet.LobbyId}");

        SendPacket(player.Id, new Packet_ConnectLobbyOk(other?.Name ?? ""));
        if(other is not null)
            SendPacket(other.Id, new Packet_LobbyNewPlayer(player.Name!));
    }

    private void HandlePacket_NewGameRequest(int peerId, Packet_NewGameRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try request new game");

        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot request game start because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY));
            return;
        }
        Lobby lobby = player.Lobby;

        if(lobby.Players[1] is null)
        {
            GD.Print($"{peerId} cannot request game start because there is no other player.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_OTHER_PLAYER));
            return;
        }
        if(lobby.Requester is not null)
        {
            GD.Print($"{peerId} cannot request game start because they already did.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID));
            return;
        }
        if(lobby.ActiveGame is not null)
        {
            GD.Print($"{peerId} cannot request game start because they are in the middle of a game.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME));
            return;
        }

        GD.Print($"{peerId} requested game start");
        lobby.Requester = player;
        Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
        SendPacket(player.Id, new Packet_NewGameRequestOk());
        SendPacket(other.Id, new Packet_NewGameRequested());
    }

    private void HandlePacket_NewGameAccept(int peerId, Packet_NewGameAccept packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try approve new game request");

        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot approve game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY));
            return;
        }
        Lobby lobby = player.Lobby;

        if(lobby.Requester is null)
        {
            GD.Print($"{peerId} cannot approve game request because there is no request");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST));
            return;
        }
        if(lobby.Requester == player)
        {
            GD.Print($"{peerId} cannot approve their own request");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_YOUR_REQUEST));
            return;
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
    }

    private void HandlePacket_NewGameReject(int peerId, Packet_NewGameReject packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try reject new game request");
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot reject game request because they are not in a lobby");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY));
            return;
        }
        Lobby lobby = player.Lobby;

        if(lobby.Requester is null)
        {
            GD.Print($"{peerId} cannot reject game request because there is no request");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST));
            return;
        }
        if(lobby.Requester == player)
        {
            GD.Print($"{peerId} cannot reject their own request");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_YOUR_REQUEST));
            return;
        }

        GD.Print($"{peerId} rejected game request");
        lobby.Requester = null;
        Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
        SendPacket(player.Id, new Packet_NewGameRejectOk());
        SendPacket(other.Id, new Packet_NewGameRejected());
    }

    private void HandlePacket_NewGameCancel(int peerId, Packet_NewGameCancel packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try cancel new game request");
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot cancel game request because they are not in a lobby");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY));
            return;
        }
        Lobby lobby = player.Lobby;

        if(lobby.Requester is null)
        {
            GD.Print($"{peerId} cannot cancel game request because there is no request");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST));
            return;
        }
        if(lobby.Requester != player)
        {
            GD.Print($"{peerId} cannot cancel the other player's request");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_YOUR_REQUEST));
            return;
        }

        GD.Print($"{peerId} canceled game request");

        lobby.Requester = null;
        Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;
        SendPacket(player.Id, new Packet_NewGameCancelOk());
        SendPacket(other.Id, new Packet_NewGameCanceled());
    }

    private void HandlePacket_LobbyDisconnect(int peerId, Packet_LobbyDisconnect packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} is disconnecting. Reason: {packet.Reason}");
        RemovePlayer(peerId, packet.Reason);
        return;
    }

    private void HandlePacket_GameActionPlace(int peerId, Packet_GameActionPlace packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try place \"{packet.ScenePath}\" at column {packet.Column}");
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} failed to place token because they are not in a game");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME));
            return;
        }
        Lobby lobby = player.Lobby;
        if(lobby.ActiveGame is null)
        {
            GD.Print($"{peerId} failed to place token because they are not in a game");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME));
            return;
        }
        Game game = lobby.ActiveGame;
        GameTurnEnum playerTurn = (lobby.Players[0] == player)?lobby.Turns![0]:lobby.Turns![1];
        if(game.Turn != playerTurn)
        {
            GD.Print($"{peerId} failed to place token because it is not their turn");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_YOUR_TURN));
            return;
        }
        int column = packet.Column;

        if(column < 0 || game.GameBoard.Columns <= column)
        {
            GD.Print($"{peerId} failed to place token because the column was invalid");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_COLUMN));
            return;
        }

        string scenePath = packet.ScenePath;

        //the packet having a null scene path is the result of an internal error
        //and NOT a bad packet
        ArgumentNullException.ThrowIfNull(scenePath);

        //no such path
        if(!ResourceLoader.Exists(scenePath))
        {
            GD.Print($"{peerId} failed to place token because the scene path does not exist");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
            return;
        }
        //load scene
        Resource res = ResourceLoader.Load(scenePath);
        //path does not point to a scene
        if(res is not PackedScene scene)
        {
            GD.Print($"{peerId} failed to place token because the scene path does not point to a scene");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
            return;
        }
        TokenBase? token = Autoloads.ScenePool.GetSceneOrNull<TokenBase>(scene);
        //scene is not a token
        if(token is null)
        {
            GD.Print($"{peerId} failed to place token because the scene is not a token");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
            return;
        }
        //find matching token counter
        TokenCounterControl? control = null;
        foreach(TokenCounterListControl lc in game.CounterLists)
        {
            if(lc.ActiveOnTurn != playerTurn)
                continue;
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
            return;
        }
        //not enough tokens to use
        if(!control.CanTake())
        {
            GD.Print($"{peerId} failed to place token because they don't have enough tokens");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS));
            return;
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
            return;
        }

        GD.Print($"{peerId} placed token");

        Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;

        SendPacket(player.Id, new Packet_GameActionPlaceOk());
        SendPacket(other.Id, new Packet_GameActionPlaceOther((byte)column, scenePath));
    }

    private void HandlePacket_GameActionRefill(int peerId, Packet_GameActionRefill packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try refill");
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} failed to refill because they are not in a game");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_NOT_IN_GAME));
            return;
        }
        Lobby lobby = player.Lobby;
        if(lobby.ActiveGame is null)
        {
            GD.Print($"{peerId} failed to refill because they are not in a game");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_NOT_IN_GAME));
            return;
        }
        Game game = lobby.ActiveGame;
        GameTurnEnum playerTurn = (lobby.Players[0] == player)?lobby.Turns![0]:lobby.Turns![1];
        if(game.Turn != playerTurn)
        {
            GD.Print($"{peerId} failed to refill because it is not their turn");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_NOT_YOUR_TURN));
            return;
        }

        bool didRefill = false;
        bool anyCouldRefill = false;
        foreach(TokenCounterListControl lc in game.CounterLists)
        {
            if(lc.ActiveOnTurn != playerTurn)
                continue;
            if(lc.AnyCanAdd())
            {
                anyCouldRefill = true;
                if(lc.DoRefill())
                    didRefill = true;
            }
        }

        if(!anyCouldRefill)
        {
            GD.Print($"{peerId} failed to refill because all were filled");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_ALL_FILLED));
        }

        if(!didRefill)
        {
            GD.Print($"{peerId} failed to refill because refilling is locked");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_TWO_TURN_STREAK));
        }

        Player other = lobby.Players[0] == player ? lobby.Players[1]! : lobby.Players[0]!;

        GD.Print($"{peerId} did refill");
        SendPacket(player.Id, new Packet_GameActionRefillOk());
        SendPacket(other.Id, new Packet_GameActionRefillOther());
    }

    #endregion

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
        ArgumentNullException.ThrowIfNull(name);

        if(name == "") name = "Guest";

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