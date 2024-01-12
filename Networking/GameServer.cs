using Godot;
using DequeNet;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        public int? Index{get; set;}

        public List<Player> RequestSources{get; set;} = new();
        public List<Player> RequestTargets{get; set;} = new();

        public Match? Match{get; set;}
        public GameTurnEnum? Turn{get; set;}
    }

    private sealed class Lobby
    {
        public uint Id{get; init;}
        public Player Leader{get; set;} = null!;
        public List<Player> Players{get; private set;} = new();
    }

    private sealed class Match
    {
        public Lobby Lobby{get; init;} = null!;
        public GameMenu Game{get; init;} = null!;
        public Player Player1{get; init;} = null!;
        public Player Player2{get; init;} = null!;
    }

    [ExportCategory("Nodes")]
    [Export]
    private WebSocketServer _server = null!;
    [ExportCategory("")]
    [Export]
    private PackedScene _gameScene = null!;
    [Export]
    public bool RefuseNewConnections{get => _server.RefuseNewConnections; set => _server.RefuseNewConnections = value;}

    private readonly Deque<byte> _buffer = new();

    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<uint, Lobby> _lobbies = new();

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_server);
        ArgumentNullException.ThrowIfNull(_gameScene);
    }

    private void ConnectSignals()
    {
        _server.PacketReceived += OnWebSocketServerPacketReceived;
        _server.ClientConnected += OnWebSocketClientConnected;
        _server.ClientDisconnected += OnWebSocketClientDisconnected;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    public Error Listen(ushort port)
    {
        Error err = _server.Listen(port);
        if(err != Error.Ok) return err;
        return Error.Ok;
    }

    public void Stop()
    {
        if(!_server.Listening) return;

        foreach(Player player in _players.Values)
        {
            if((player.Match?.Game).IsInstanceValid()) player.Match.Game.QueueFree();
            SendPacket(player.Id, new Packet_ServerClosing());
        }

        _server.Stop();
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
        _server.SendPacket(peerId, packet.ToByteArray());
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
                GD.PushError($"Server did not expect packet of type {packet.GetType().Name} from {peerId}");
                break;
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

        if(packet.PlayerName == "") packet.PlayerName = "Guest";

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
        lobby.Players.Add(player);
        lobby.Leader = player;
        player.Lobby = lobby;
        player.Index = 0;
        _lobbies[id] = lobby;
        GD.Print($"Created lobby {id} for {peerId}");

        //respond
        SendPacket(peerId, new Packet_CreateLobbyOk(id));
    }

    private void HandlePacket_ConnectLobbyRequest(int peerId, Packet_ConnectLobbyRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);

        if(packet.PlayerName == "") packet.PlayerName = "Guest";
        
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

        lobby.Players.Add(player);

        GD.Print($"{peerId} connected to lobby {packet.LobbyId}");

        int index = lobby.Players.Count - 1;
        string[] names = lobby.Players.Select(player => player.Name).ToArray();
        SendPacket(player.Id, new Packet_ConnectLobbyOk(index, names));
        foreach(Player other in lobby.Players) if(other != player)
        {
            SendPacket(other.Id, new Packet_LobbyNewPlayer(player.Name));
        }
    }

    private void HandlePacket_NewGameRequest(int peerId, Packet_NewGameRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try request new game on player number {packet.RequestTargetIndex}");

        int playerIndex = packet.RequestTargetIndex;
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot request game start because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY, playerIndex));
            return;
        }
        Lobby lobby = player.Lobby;
        if(playerIndex == player.Index)
        {
            GD.Print($"{peerId} cannot request game start on themselves.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_YOURSELF, playerIndex));
            return;
        }

        if(playerIndex < 0 || lobby.Players.Count <= playerIndex)
        {
            GD.Print($"{peerId} cannot request game start with invalid player.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_INVALID_PLAYER, playerIndex));
            return;
        }

        Player other = lobby.Players[playerIndex];

        if(player.RequestTargets.Contains(other))
        {
            GD.Print($"{peerId} cannot request game start because they already did.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID, playerIndex));
            return;
        }

        if(player.RequestSources.Contains(other))
        {
            GD.Print($"{peerId} cannot request game start because there's already an active request.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_OTHER_DID, playerIndex));
            return;
        }

        if(player.Match is not null)
        {
            GD.Print($"{peerId} cannot request game start because they are in the middle of a game.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME, playerIndex));
            return;
        }

        if(other.Match is not null)
        {
            GD.Print($"{peerId} cannot request game start from {playerIndex} because that player is in the middle of a game.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME_OTHER, playerIndex));
            return;
        }

        GD.Print($"{peerId} requested game start to {other.Id}");
        other.RequestSources.Add(player);
        player.RequestTargets.Add(player);
        foreach(Player another in lobby.Players)
            SendPacket(another.Id, new Packet_NewGameRequested((int)player.Index!, (int)other.Index!));
    }

    private void HandlePacket_NewGameAccept(int peerId, Packet_NewGameAccept packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try approve new game request from player number {packet.RequestSourceIndex}");

        int playerIndex = packet.RequestSourceIndex;
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot approve game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY, playerIndex));
            return;
        }
        Lobby lobby = player.Lobby;

        if(playerIndex < 0 || lobby.Players.Count <= playerIndex)
        {
            GD.Print($"{peerId} cannot approve game request from invalid player.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST, playerIndex));
            return;
        }
        Player other = lobby.Players[playerIndex];

        if(!player.RequestTargets.Contains(other))
        {
            GD.Print($"{peerId} cannot approve game request because there is no request");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST, playerIndex));
            return;
        }

        GD.Print($"{peerId} approved game request from {other.Id}");

        other.RequestSources.Remove(player);
        player.RequestTargets.Remove(other);

        foreach(Player another in lobby.Players)
            SendPacket(another.Id, new Packet_NewGameAccepted((int)other.Index!, (int)player.Index!));
        /*
        start game here
        */
        GD.Print($"game will now started in lobby {lobby.Id} with {player.Id} and {other.Id}");
        bool which = GD.RandRange(0, 1) == 0; //decide which player is first
        Match match = new()
        {
            Lobby = lobby,
            Game = Autoloads.ScenePool.GetScene<GameMenu>(_gameScene!),
            Player1 = which ? player : other,
            Player2 = which ? other : player
        };
        AddChild(match.Game);
        
        player.Match = match; player.Turn = which ? GameTurnEnum.Player1 : GameTurnEnum.Player2;
        other.Match = match; other.Turn = which ? GameTurnEnum.Player2 : GameTurnEnum.Player1;
        
        foreach(Player another in lobby.Players)
            SendPacket(another.Id, new Packet_NewGameStarting((int)match.Player1.Index!, (int)match.Player2.Index!));
    }

    private void HandlePacket_NewGameReject(int peerId, Packet_NewGameReject packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try reject new game request from player number {packet.RequestSourceIndex}");

        int playerIndex = packet.RequestSourceIndex;
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot reject game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY, playerIndex));
            return;
        }
        Lobby lobby = player.Lobby;

        if(playerIndex < 0 || lobby.Players.Count <= playerIndex)
        {
            GD.Print($"{peerId} cannot reject game request from invalid player.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST, playerIndex));
            return;
        }
        Player other = lobby.Players[playerIndex];

        if(!player.RequestTargets.Contains(other))
        {
            GD.Print($"{peerId} cannot reject game request because there is no request");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST, playerIndex));
            return;
        }

        GD.Print($"{peerId} rejected game request from {other.Id}");

        other.RequestSources.Remove(player);
        player.RequestTargets.Remove(other);

        foreach(Player another in lobby.Players)
            SendPacket(another.Id, new Packet_NewGameRejected((int)other.Index!, (int)player.Index!));
    }

    private void HandlePacket_NewGameCancel(int peerId, Packet_NewGameCancel packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try cancel new game request to player number {packet.RequestTargetIndex}");

        int playerIndex = packet.RequestTargetIndex;
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} cannot cancel game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY, playerIndex));
            return;
        }
        Lobby lobby = player.Lobby;

        if(playerIndex < 0 || lobby.Players.Count <= playerIndex)
        {
            GD.Print($"{peerId} cannot cancel game request to invalid player.");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST, playerIndex));
            return;
        }
        Player other = lobby.Players[playerIndex];

        if(!player.RequestTargets.Contains(other))
        {
            GD.Print($"{peerId} cannot cancel game request because there is no request");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST, playerIndex));
            return;
        }

        GD.Print($"{peerId} canceled game request to {other.Id}");

        other.RequestSources.Remove(player);
        player.RequestTargets.Remove(other);

        foreach(Player another in lobby.Players)
            SendPacket(another.Id, new Packet_NewGameCanceled((int)player.Index!, (int)other.Index!));
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
        Match? match = player.Match;
        if(match is null)
        {
            GD.Print($"{peerId} failed to place token because they are not in a game");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME));
            return;
        }
        GameMenu game = match.Game;
        GameTurnEnum playerTurn = (GameTurnEnum)player.Turn!;
        if(game.Turn != playerTurn)
        {
            GD.Print($"{peerId} failed to place token because it is not their turn");
            SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_YOUR_TURN));
            return;
        }
        int column = packet.Column;

        if(!game.ValidColumn(column))
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
    
        ErrorCodeEnum? placeError = game.PlaceToken(column, scene);

        switch(placeError)
        {
            case ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN:
                GD.Print($"{peerId} failed to place token because the token is invalid");
                SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN));
                return;
            case ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS:
                GD.Print($"{peerId} failed to place token because they don't have enough tokens");
                SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS));
                return;
            case ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN:
                GD.Print($"{peerId} failed to place token because the column is full");
                SendPacket(peerId, new Packet_GameActionPlaceFail(ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN));
                return;
        }

        GD.Print($"{peerId} placed token");

        Player other = match.Player1 == player ? match.Player2 : match.Player1;

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
        Match? match = player.Match;
        if(match is null)
        {
            GD.Print($"{peerId} failed to refill because they are not in a game");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_NOT_IN_GAME));
            return;
        }
        GameMenu game = match.Game;
        GameTurnEnum playerTurn = (GameTurnEnum)player.Turn!;
        if(game.Turn != playerTurn)
        {
            GD.Print($"{peerId} failed to refill because it is not their turn");
            SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_NOT_YOUR_TURN));
            return;
        }

        ErrorCodeEnum? refillError = game.Refill();

        switch(refillError)
        {
            case ErrorCodeEnum.CANNOT_REFILL_ALL_FILLED:
                GD.Print($"{peerId} failed to refill because all were filled");
                SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_ALL_FILLED));
                return;
            case ErrorCodeEnum.CANNOT_REFILL_TWO_TURN_STREAK:
                GD.Print($"{peerId} failed to refill because refilling is locked");
                SendPacket(peerId, new Packet_GameActionRefillFail(ErrorCodeEnum.CANNOT_REFILL_TWO_TURN_STREAK));
                return;
        }

        Player other = match.Player1 == player ? match.Player2 : match.Player1;

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

        Match? match = player.Match;
        if(match is not null)
        {
            Autoloads.ScenePool.ReturnScene(match.Game);
            Player opponent = match.Player1 == player ? match.Player2 : match.Player1;
            opponent.Match = null;
            opponent.Turn = null;
        }
        player.Match = null;
        player.Turn = null;
        player.RequestSources.Clear();
        player.RequestTargets.Clear();

        int index = (int)player.Index!;
        foreach(Player other in lobby.Players) if(other != player)
        {
            other.RequestSources.Remove(player);
            other.RequestTargets.Remove(player);
            SendPacket(other.Id, new Packet_LobbyDisconnectOther(reason, index));
        }
        player.Index = null;

        lobby.Players.RemoveAt(index);
        //lobby is now empty
        if(lobby.Players.Count == 0)
        {
            _lobbies.Remove(lobby.Id);
        }
        else
        {
            lobby.Leader = lobby.Players[0];
        }
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