using Godot;
using DequeNet;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FourInARowBattle;

/// <summary>
/// This class is a mediator between the low level WebSocketServer and high level GameServerMenu.
///
/// Because servers have much less interaction needed, this class is mainly
/// to separate the UI logic.
/// </summary>
public partial class GameServer : Node
{
    /// <summary>
    /// An internal class used to store player data
    /// </summary>
    private sealed class Player
    {
        public Player(int id, string name)
        {
            Id = id;
            Name = name;
            Lobby = null;
        }
        /// <summary>
        /// The player id
        /// </summary>
        public int Id{get; init;}
        /// <summary>
        /// The player name
        /// </summary>
        public string Name{get; set;}

        /// <summary>
        /// The player lobby, if they are in one
        /// </summary>
        public Lobby? Lobby{get; set;}
        /// <summary>
        /// The player index in the lobby, if they are in one
        /// </summary>
        public int? Index{get; set;}

        /// <summary>
        /// List of players that sent requests to this player
        /// </summary>
        public List<Player> RequestSources{get; set;} = new();
        /// <summary>
        /// List of players that this player sent requests to
        /// </summary>
        public List<Player> RequestTargets{get; set;} = new();

        /// <summary>
        /// The match the player is playing, if they are
        /// </summary>
        public Match? Match{get; set;}
        /// <summary>
        /// The players' turn in the match, if they are in one
        /// </summary>
        public GameTurnEnum? Turn{get; set;}
    }

    /// <summary>
    /// An internal class used to store lobby data
    /// </summary>
    private sealed class Lobby
    {
        /// <summary>
        /// The lobby id
        /// </summary>
        public uint Id{get; init;}
        /// <summary>
        /// The players in the lobby
        /// </summary>
        public List<Player> Players{get; private set;} = new();
    }

    /// <summary>
    /// An internal class used to stoer match data
    /// </summary>
    private sealed class Match
    {
        /// <summary>
        /// The lobby the match takes place in
        /// </summary>
        public Lobby Lobby{get; init;} = null!;
        /// <summary>
        /// The game that is being played
        /// </summary>
        public GameMenu Game{get; init;} = null!;
        /// <summary>
        /// Player 1
        /// </summary>
        public Player Player1{get; init;} = null!;
        /// <summary>
        /// Player 2
        /// </summary>
        public Player Player2{get; init;} = null!;
    }

    [ExportCategory("Nodes")]
    [Export]
    private WebSocketServer _server = null!;
    [ExportCategory("")]
    [Export]
    private PackedScene _gameScene = null!;
    [Export]
    private GameData _initialState = null!;
    [Export]
    public bool RefuseNewConnections{get => _server.RefuseNewConnections; set => _server.RefuseNewConnections = value;}

    private readonly Deque<byte> _buffer = new();

    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<uint, Lobby> _lobbies = new();

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_server);
        ArgumentNullException.ThrowIfNull(_gameScene);
        ArgumentNullException.ThrowIfNull(_initialState);
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

    /// <summary>
    /// Start listening for new connections on the given port
    /// </summary>
    /// <param name="port">The port</param>
    public Error Listen(ushort port)
    {
        Error err = _server.Listen(port);
        if(err != Error.Ok)
        {
            GD.Print($"Listening on port {port} failed with error {err}");
            return err;
        }
        return Error.Ok;
    }

    /// <summary>
    /// Stop listening for new connections
    /// </summary>
    public void Stop()
    {
        if(!_server.Listening) return;

        //free games
        foreach(Player player in _players.Values)
        {
            if((player.Match?.Game).IsInstanceValid())
                player.Match.Game.QueueFree();
        }

        _players.Clear();
        _lobbies.Clear();
        _buffer.Clear();

        //broadcast
        SendPacket(0, new Packet_ServerClosing());

        _server.Stop();
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            Stop();
        }
    }

    /// <summary>
    /// Received packet
    /// </summary>
    /// <param name="peerId">The id of the peer that sent the packet</param>
    /// <param name="packetBytes">The packet bytes</param>
    private void OnWebSocketServerPacketReceived(int peerId, byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        //add to buffer
        _buffer.PushRightRange(packetBytes);
        //create packets while possible
        while(_buffer.Count > 0 && AbstractPacket.TryConstructPacketFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(peerId, packet);
        }
    }

    /// <summary>
    /// New client connected
    /// </summary>
    /// <param name="peerId">The id of the new client</param>
    private void OnWebSocketClientConnected(int peerId)
    {
        GD.Print($"New client {peerId}");
        _players[peerId] = new Player(peerId, "");
    }

    /// <summary>
    /// Client disconnected
    /// </summary>
    /// <param name="peerId">The id of the disconnected client</param>
    private void OnWebSocketClientDisconnected(int peerId)
    {
        GD.Print($"Client {peerId} disconnected");
        RemovePlayer(peerId, DisconnectReasonEnum.CONNECTION);
    }

    #region Packet Handling

    /// <summary>
    /// Send a packet
    /// </summary>
    /// <param name="peerId">The id to send to</param>
    /// <param name="packet">The packet to send</param>
    private void SendPacket(int peerId, AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        _server.SendPacket(peerId, packet.ToByteArray());
    }

    /// <summary>
    /// Handle a packet
    /// </summary>
    /// <param name="peerId">The id that sent the packet</param>
    /// <param name="packet">The packet</param>
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
            case Packet_GameQuit _packet:
                HandlePaket_GameQuit(peerId, _packet);
                break;
            default:
                GD.PushError($"Server did not expect packet of type {packet.GetType().Name} from {peerId}");
                break;
        }
    }

    /// <summary>
    /// Handle dummy packet
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private static void HandlePacket_Dummy(int peerId, Packet_Dummy packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got dummy packet from {peerId}");
    }

    /// <summary>
    /// Handle invalid packet
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_InvalidPacket(int peerId, Packet_InvalidPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got invalid packet from {peerId}: {packet.GivenPacketType}");
        SendPacket(peerId, new Packet_InvalidPacketInform(packet.GivenPacketType));
    }

    /// <summary>
    /// Handle lobby creation request
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_CreateLobbyRequest(int peerId, Packet_CreateLobbyRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);

        string name = packet.PlayerName;
        if(name == "") name = "Guest";

        GD.Print($"{peerId} wants to create lobby. Player name: {name}");

        if(!TryUpdateNameAndGetPlayer(peerId, name, out Player? player))
        {
            GD.Print($"{peerId} failed to create lobby. They are already in one.");
            SendPacket(peerId, new Packet_CreateLobbyFail(ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY));
            return;
        }

        //create lobby
        uint id; do{id = GD.Randi();} while(_lobbies.ContainsKey(id));
        Lobby lobby = new(){Id = id};
        lobby.Players.Add(player);
        player.Lobby = lobby;
        player.Index = 0;
        _lobbies[id] = lobby;
        GD.Print($"Created lobby {id} for {peerId}");

        //respond
        SendPacket(peerId, new Packet_CreateLobbyOk(id));
    }

    /// <summary>
    /// Handle lobby connection request
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_ConnectLobbyRequest(int peerId, Packet_ConnectLobbyRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);

        string name = packet.PlayerName;
        if(name == "") name = "Guest";

        GD.Print($"{peerId} wants to connect to lobby {packet.LobbyId} with name {name}");

        if(!TryUpdateNameAndGetPlayer(peerId, name, out Player? player))
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

        GD.Print($"{peerId} connected to lobby {packet.LobbyId}");

        lobby.Players.Add(player);
        int index = lobby.Players.Count - 1;
        IEnumerable<string> names = lobby.Players.Select(player => player.Name);
        //create lobby data
        LobbyPlayerData[] data = lobby.Players
            .Select(player => new LobbyPlayerData(player.Name, player.Match is not null))
            .ToArray();

        player.Lobby = lobby;
        player.Index = index;
        //send approval to player
        SendPacket(player.Id, new Packet_ConnectLobbyOk(index, data));
        foreach(Player other in lobby.Players) if(other != player)
        {
            //inform others of new player
            SendPacket(other.Id, new Packet_LobbyNewPlayer(player.Name));
        }
    }

    /// <summary>
    /// Handle game request being sent
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRequest(int peerId, Packet_NewGameRequest packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try request new game on player number {packet.RequestTargetIndex}");

        int targetIdx = packet.RequestTargetIndex;
        if(!_players.TryGetValue(peerId, out Player? source) || source.Lobby is null)
        {
            GD.Print($"{peerId} cannot request game start because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY, targetIdx));
            return;
        }
        Lobby lobby = source.Lobby;
        if(targetIdx == source.Index)
        {
            GD.Print($"{peerId} cannot request game start on themselves.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_YOURSELF, targetIdx));
            return;
        }

        if(targetIdx < 0 || lobby.Players.Count <= targetIdx)
        {
            GD.Print($"{peerId} cannot request game start with invalid player.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_INVALID_PLAYER, targetIdx));
            return;
        }

        Player target = lobby.Players[targetIdx];

        if(source.RequestTargets.Contains(target))
        {
            GD.Print($"{peerId} cannot request game start because they already did.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID, targetIdx));
            return;
        }

        if(source.RequestSources.Contains(target))
        {
            GD.Print($"{peerId} cannot request game start because the other person did.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_OTHER_DID, targetIdx));
            return;
        }

        if(source.Match is not null)
        {
            GD.Print($"{peerId} cannot request game start because they are in the middle of a game.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME, targetIdx));
            return;
        }

        if(target.Match is not null)
        {
            GD.Print($"{peerId} cannot request game start from {targetIdx} because that player is in the middle of a game.");
            SendPacket(peerId, new Packet_NewGameRequestFail(ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME_OTHER, targetIdx));
            return;
        }

        GD.Print($"{peerId} requested game start to {target.Id}");
        target.RequestSources.Add(source);
        source.RequestTargets.Add(target);
        SendPacket(source.Id, new Packet_NewGameRequestOk((int)target.Index!));
        SendPacket(target.Id, new Packet_NewGameRequested((int)source.Index!));
    }

    /// <summary>
    /// Handle game request accept
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameAccept(int peerId, Packet_NewGameAccept packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try approve new game request from player number {packet.RequestSourceIndex}");

        int sourceIdx = packet.RequestSourceIndex;
        if(!_players.TryGetValue(peerId, out Player? target) || target.Lobby is null)
        {
            GD.Print($"{peerId} cannot approve game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY, sourceIdx));
            return;
        }
        Lobby lobby = target.Lobby;

        if(sourceIdx < 0 || lobby.Players.Count <= sourceIdx)
        {
            GD.Print($"{peerId} cannot approve game request from invalid player.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_INVALID_PLAYER, sourceIdx));
            return;
        }
        Player source = lobby.Players[sourceIdx];

        if(!target.RequestSources.Contains(source))
        {
            GD.Print($"{peerId} cannot approve game request because there is no request");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST, sourceIdx));
            return;
        }

        GD.Print($"{peerId} approved game request from {source.Id}");

        //clear requestse
        source.RequestSources.Clear();
        source.RequestTargets.Clear();
        target.RequestSources.Clear();
        target.RequestTargets.Clear();
        //send approvals
        SendPacket(source.Id, new Packet_NewGameAccepted((int)target.Index!));
        SendPacket(target.Id, new Packet_NewGameAcceptOk((int)source.Index!));
        
        //tell other players in lobby that those players become busy
        Packet_LobbyPlayerBusyTrue sourceBusy = new((int)source.Index!);
        Packet_LobbyPlayerBusyTrue targetBusy = new((int)target.Index!);
        foreach(Player another in lobby.Players)
        {
            if(another != source && another != target)
            {
                another.RequestSources.Remove(source);
                another.RequestSources.Remove(target);
                another.RequestTargets.Remove(source);
                another.RequestTargets.Remove(target);
                SendPacket(another.Id, sourceBusy);
                SendPacket(another.Id, targetBusy);
            }
        }

        //now we start the game

        GD.Print($"game will now started in lobby {lobby.Id} with {target.Id} and {source.Id}");
        //decide which player is first
        bool which = GD.RandRange(0, 1) == 0;
        //create match object
        Match match = new()
        {
            Lobby = lobby,
            Game = Autoloads.ScenePool.GetScene<GameMenu>(_gameScene),
            Player1 = which ? target : source,
            Player2 = which ? source : target
        };
        //make game invisible
        match.Game.Visible = false;
        //add game to scene
        AddChild(match.Game);
        //set to initial state
        match.Game.DeserializeFrom(_initialState);

        //send game start packets
        GameTurnEnum targetTurn = which ? GameTurnEnum.PLAYER1 : GameTurnEnum.PLAYER2;
        GameTurnEnum sourceTurn = which ? GameTurnEnum.PLAYER2 : GameTurnEnum.PLAYER1;
        target.Match = match; target.Turn = targetTurn;
        source.Match = match; source.Turn = sourceTurn;
        SendPacket(source.Id, new Packet_NewGameStarting(sourceTurn, (int)target.Index!));
        SendPacket(target.Id, new Packet_NewGameStarting(targetTurn, (int)source.Index!));
    }

    /// <summary>
    /// Handle game request reject
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameReject(int peerId, Packet_NewGameReject packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try reject new game request from player number {packet.RequestSourceIndex}");

        int sourceIdx = packet.RequestSourceIndex;
        if(!_players.TryGetValue(peerId, out Player? target) || target.Lobby is null)
        {
            GD.Print($"{peerId} cannot reject game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY, sourceIdx));
            return;
        }
        Lobby lobby = target.Lobby;

        if(sourceIdx < 0 || lobby.Players.Count <= sourceIdx)
        {
            GD.Print($"{peerId} cannot reject game request from invalid player.");
            SendPacket(peerId, new Packet_NewGameAcceptFail(ErrorCodeEnum.CANNOT_REJECT_INVALID_PLAYER, sourceIdx));
            return;
        }
        Player source = lobby.Players[sourceIdx];

        if(!target.RequestSources.Contains(source))
        {
            GD.Print($"{peerId} cannot reject game request because there is no request");
            SendPacket(peerId, new Packet_NewGameRejectFail(ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST, sourceIdx));
            return;
        }

        GD.Print($"{peerId} rejected game request from {source.Id}");

        source.RequestTargets.Remove(target);
        target.RequestSources.Remove(source);

        SendPacket(source.Id, new Packet_NewGameRejected((int)target.Index!));
        SendPacket(target.Id, new Packet_NewGameRejectOk((int)source.Index!));
    }

    /// <summary>
    /// Handle game request cancel
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameCancel(int peerId, Packet_NewGameCancel packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} try cancel new game request to player number {packet.RequestTargetIndex}");

        int targetIdx = packet.RequestTargetIndex;
        if(!_players.TryGetValue(peerId, out Player? source) || source.Lobby is null)
        {
            GD.Print($"{peerId} cannot cancel game request because they are not in a lobby.");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY, targetIdx));
            return;
        }
        Lobby lobby = source.Lobby;

        if(targetIdx < 0 || lobby.Players.Count <= targetIdx)
        {
            GD.Print($"{peerId} cannot cancel game request to invalid player.");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_INVALID_PLAYER, targetIdx));
            return;
        }
        Player target = lobby.Players[targetIdx];

        if(!source.RequestTargets.Contains(target))
        {
            GD.Print($"{peerId} cannot cancel game request because there is no request");
            SendPacket(peerId, new Packet_NewGameCancelFail(ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST, targetIdx));
            return;
        }

        GD.Print($"{peerId} canceled game request to {target.Id}");

        target.RequestSources.Remove(source);
        source.RequestTargets.Remove(target);

        SendPacket(source.Id, new Packet_NewGameCancelOk((int)target.Index!));
        SendPacket(target.Id, new Packet_NewGameCanceled((int)source.Index!));
    }

    /// <summary>
    /// Handle lobby disconnect
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyDisconnect(int peerId, Packet_LobbyDisconnect packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} is disconnecting. Reason: {packet.Reason}");
        RemovePlayer(peerId, packet.Reason);
        return;
    }

    /// <summary>
    /// Handle token placing
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
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

        //try placing the token
        ErrorCodeEnum? placeError = game.PlaceToken(column, scene);
        //got an error
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

    /// <summary>
    /// Handle refill
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
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
        //try refill
        ErrorCodeEnum? refillError = game.Refill();
        //got error
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

    /// <summary>
    /// Handle game quit
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="packet">The packet</param>
    private void HandlePaket_GameQuit(int peerId, Packet_GameQuit packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"{peerId} quit game");
        if(!_players.TryGetValue(peerId, out Player? player) || player.Lobby is null)
        {
            GD.Print($"{peerId} failed to quit because they are not in a game");
            SendPacket(peerId, new Packet_GameQuitFail(ErrorCodeEnum.CANNOT_QUIT_NOT_IN_GAME));
            return;
        }
        Match? match = player.Match;
        if(match is null)
        {
            GD.Print($"{peerId} failed to quit because they are not in a game");
            SendPacket(peerId, new Packet_GameQuitFail(ErrorCodeEnum.CANNOT_QUIT_NOT_IN_GAME));
            return;
        }
        Player other = (match.Player1 == player) ? match.Player2 : match.Player1;
        QuitMatch(match);
        SendPacket(player.Id, new Packet_GameQuitOk());
        SendPacket(other.Id, new Packet_GameQuitOther());
    }

    #endregion

    /// <summary>
    /// Remove player that disconnected from lobby
    /// </summary>
    /// <param name="peerId">The sender</param>
    /// <param name="reason">The disconnect reason</param>
    private void RemovePlayer(int peerId, DisconnectReasonEnum reason)
    {
        if(!_players.TryGetValue(peerId, out Player? player)) return;
        _players.Remove(player.Id);
        Lobby? lobby = player.Lobby;
        if(lobby is null) return;
        //player was already removed
        if(!lobby.Players.Contains(player)) return;
        //got a match. clean up.
        Match? match = player.Match;
        if(match is not null)
            QuitMatch(match);
        //infor, players in lobby
        int index = (int)player.Index!;
        Packet_LobbyDisconnectOther disconnectPacket = new(reason, index);
        foreach(Player other in lobby.Players) if(other != player)
        {
            other.RequestSources.Remove(player);
            other.RequestTargets.Remove(player);
            SendPacket(other.Id, disconnectPacket);
        }
        player.Index = null;
        //remove from players
        lobby.Players.RemoveAt(index);
        //update index
        for(int i = 0; i < lobby.Players.Count; ++i)
        {
            lobby.Players[i].Index = i;
        }
        //lobby is now empty
        if(lobby.Players.Count == 0)
        {
            _lobbies.Remove(lobby.Id);
        }
    }

    /// <summary>
    /// Clean up a match that ended
    /// </summary>
    /// <param name="match">The match</param>
    private void QuitMatch(Match match)
    {
        //dispose game
        Autoloads.ScenePool.ReturnScene(match.Game);
        //clean references
        Player player1 = match.Player1;
        Player player2 = match.Player2;
        player1.Match = null;
        player1.Turn = null;
        player2.Match = null;
        player2.Turn = null;
        //inform others they're not busy
        Packet_LobbyPlayerBusyFalse notBusy1 = new((int)player1.Index!);
        Packet_LobbyPlayerBusyFalse notBusy2 = new((int)player2.Index!);
        foreach(Player other in match.Lobby.Players)
        {
            if(other != player1 && other != player2)
            {
                SendPacket(other.Id, notBusy1);
                SendPacket(other.Id, notBusy2);
            }
        }
        //clear requests
        player1.RequestSources.Clear();
        player1.RequestTargets.Clear();
        player2.RequestSources.Clear();
        player2.RequestTargets.Clear();
    }

    /// <summary>
    /// Helper function. Update player name and get it if that player is in a lobby.
    /// </summary>
    /// <param name="peerId">The player id</param>
    /// <param name="name">The player name</param>
    /// <param name="player">Out param for player</param>
    /// <returns>Whether the player is in a lobby</returns>
    private bool TryUpdateNameAndGetPlayer(int peerId, string name, [NotNullWhen(true)] out Player? player)
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