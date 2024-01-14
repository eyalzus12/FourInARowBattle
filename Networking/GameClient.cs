using Godot;
using DequeNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

/// <summary>
/// This is a class that serves as a mediator between the low level WebSocketClient and
/// the high level GameClientMenu.
/// 
/// The class converts the packets into objects, does the needed actions, and sends signals
/// to the UI to display things.
/// 
/// The UI in turn calls functions that the class error-checks, converts to packets, and send.
/// </summary>
public partial class GameClient : Node
{
    #region Signals

    /// <summary>
    /// Connected to server
    /// </summary>
    [Signal]
    public delegate void ConnectedEventHandler();
    /// <summary>
    /// Disconnected from server
    /// </summary>
    [Signal]
    public delegate void DisconnectedEventHandler();
    /// <summary>
    /// Server closed
    /// </summary>
    [Signal]
    public delegate void ServerClosedEventHandler();
    /// <summary>
    /// An error occured and should be displayed to the screen
    /// </summary>
    /// <param name="description">The description of the error</param>
    [Signal]
    public delegate void ErrorOccuredEventHandler(string description);
    /// <summary>
    /// Lobby was entered
    /// </summary>
    /// <param name="lobbyId">The id of the lobby</param>
    /// <param name="players">The data of the players in the lobby</param>
    /// <param name="index">Our index inside the lobby</param>
    [Signal]
    public delegate void LobbyEnteredEventHandler(uint lobbyId, LobbyPlayerData[] players, int index);
    /// <summary>
    /// A player left the lobby
    /// </summary>
    /// <param name="index">The index of the player who left</param>
    [Signal]
    public delegate void LobbyPlayerLeftEventHandler(int index);
    /// <summary>
    /// A player joined the lobby
    /// </summary>
    /// <param name="name">The name of the player who joined</param>
    [Signal]
    public delegate void LobbyPlayerJoinedEventHandler(string name);
    /// <summary>
    /// Lobby will timeout soon
    /// </summary>
    /// <param name="secondsRemaining">Seconds until lobby times out</param>
    [Signal]
    public delegate void LobbyTimeoutWarnedEventHandler(int secondsRemaining);
    /// <summary>
    /// Lobby timed out
    /// </summary>
    [Signal]
    public delegate void LobbyTimedOutEventHandler();
    /// <summary>
    /// Opponent quit the game
    /// </summary>
    [Signal]
    public delegate void GameQuitByOpponentEventHandler();
    /// <summary>
    /// We quit the game
    /// </summary>
    [Signal]
    public delegate void GameQuitBySelfEventHandler();
    /// <summary>
    /// New game request sent succesfully
    /// </summary>
    /// <param name="playerIndex">The player it was sent to</param>
    [Signal]
    public delegate void NewGameRequestSentEventHandler(int playerIndex);
    /// <summary>
    /// New game request received
    /// </summary>
    /// <param name="playerIndex">The player who sent it</param>
    [Signal]
    public delegate void NewGameRequestReceivedEventHandler(int playerIndex);
    /// <summary>
    /// Game request approval sent
    /// </summary>
    /// <param name="playerIndex">The player who made the request</param>
    [Signal]
    public delegate void NewGameAcceptSentEventHandler(int playerIndex);
    /// <summary>
    /// Received game request approval
    /// </summary>
    /// <param name="playerIndex">The player who accepted</param>
    [Signal]
    public delegate void NewGameAcceptReceivedEventHandler(int playerIndex);
    /// <summary>
    /// Game request rejection sent
    /// </summary>
    /// <param name="playerIndex">The player who made the request</param>
    [Signal]
    public delegate void NewGameRejectSentEventHandler(int playerIndex);
    /// <summary>
    /// Received game request rejection
    /// </summary>
    /// <param name="playerIndex">The player who rejected</param>
    [Signal]
    public delegate void NewGameRejectReceivedEventHandler(int playerIndex);
    /// <summary>
    /// Game request cancelation sent
    /// </summary>
    /// <param name="playerIndex">The player it was sent to</param>
    [Signal]
    public delegate void NewGameCancelSentEventHandler(int playerIndex);
    /// <summary>
    /// Received game request cancelation
    /// </summary>
    /// <param name="playerIndex">The player who canceled</param>
    [Signal]
    public delegate void NewGameCancelReceivedEventHandler(int playerIndex);
    /// <summary>
    /// Player is now in a game
    /// </summary>
    /// <param name="playerIndex">The player who is in a game</param>
    [Signal]
    public delegate void PlayerBecameBusyEventHandler(int playerIndex);
    /// <summary>
    /// Player is no longer in game
    /// </summary>
    /// <param name="playerIndex">The player who is no longer in a game</param>
    [Signal]
    public delegate void PlayerBecameAvailableEventHandler(int playerIndex);
    /// <summary>
    /// Game started
    /// </summary>
    /// <param name="turn">Our turn</param>
    /// <param name="opponentIndex">The opponent</param>
    [Signal]
    public delegate void GameStartedEventHandler(GameTurnEnum turn, int opponentIndex);
    /// <summary>
    /// Placing token was succesful
    /// </summary>
    /// <param name="column">The column it was placed in</param>
    /// <param name="scene">The token scene</param>
    [Signal]
    public delegate void GameActionPlaceSentEventHandler(int column, PackedScene scene);
    /// <summary>
    /// Opponent placed token
    /// </summary>
    /// <param name="column">The column it was placed in</param>
    /// <param name="scene">The token scene</param>
    [Signal]
    public delegate void GameActionPlaceReceivedEventHandler(int column, PackedScene scene);
    /// <summary>
    /// Refilling was succesful
    /// </summary>
    [Signal]
    public delegate void GameActionRefillSentEventHandler();
    /// <summary>
    /// Opponent refilled
    /// </summary>
    [Signal]
    public delegate void GameActionRefillReceivedEventHandler();
    /// <summary>
    /// Game finished. This packet type is unused.
    /// </summary>
    [Signal]
    public delegate void GameFinishedEventHandler();

    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private WebSocketClient _client = null!;

    private readonly Deque<byte> _buffer = new();

    public string ClientName{get; set;} = "";

    #region State Variables

    /// <summary>
    /// Internal class used to keep player state while inside lobby
    /// </summary>
    private sealed class Player
    {
        /// <summary>
        /// The name of the player
        /// </summary>
        public string Name{get; set;} = "";
        /// <summary>
        /// The index of the player
        /// </summary>
        public int Index{get; set;}
        /// <summary>
        /// Whether we sent a game request to the player
        /// </summary>
        public bool IMadeRequest{get; set;} = false;
        /// <summary>
        /// Whether the player sent a game request
        /// </summary>
        public bool IGotRequest{get; set;} = false;
        /// <summary>
        /// Whether the player is in a game
        /// </summary>
        public bool Busy{get; set;} = false;
        /// <summary>
        /// Whether a game request was sent to that player
        /// </summary>
        public bool GameRequestSent{get; set;}
        /// <summary>
        /// Whether a game accept was sent to that player
        /// </summary>
        public bool GameAcceptSent{get; set;}
        /// <summary>
        /// Whether a game reject was sent to that player
        /// </summary>
        public bool GameRejectSent{get; set;}
        /// <summary>
        /// Whether a game cancel was sent to that player
        /// </summary>
        public bool GameCancelSent{get; set;}
    }

    /// <summary>
    /// Internal class used to store lobby data
    /// </summary>
    private sealed class Lobby
    {
        /// <summary>
        /// The lobby id
        /// </summary>
        public uint Id{get; set;}
        /// <summary>
        /// The players in the lobby
        /// </summary>
        public List<Player> Players{get; set;} = new();
        /// <summary>
        /// The game opponent if it exists
        /// </summary>
        public Player? Opponent{get; set;} = null;
        /// <summary>
        /// Our index inside the lobby
        /// </summary>
        public int Index{get; set;}
    }

    private Lobby? _lobby = null;

    //we store the packet itself so we can know the lobby id we connected to
    private Packet_ConnectLobbyRequest? _lobbyConnectionPacket = null;
    //we store the packet itself so we can know the column and token we placed
    private Packet_GameActionPlace? _placePacket = null;
    //we don't need to store data for those two. so they're just booleans.
    private bool _refillSent = false;
    private bool _quitSent = false;

    #endregion

    /// <summary>
    /// Error if any non-null exported nodes are null
    /// </summary>
    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_client);
    }

    /// <summary>
    /// Connect signals
    /// </summary>
    private void ConnectSignals()
    {
        _client.PacketReceived += OnWebSocketClientPacketReceived;
        _client.ConnectedToServer += OnWebSocketClientConnected;
        _client.ConnectionClosed += OnWebSocketClientConnectionClosed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    /// <summary>
    /// Connect to a server
    /// </summary>
    /// <param name="ip">The server IP</param>
    /// <param name="_port">The port to connect to</param>
    public Error ConnectToServer(string ip, string _port)
    {
        ArgumentNullException.ThrowIfNull(ip);
        ArgumentNullException.ThrowIfNull(_port);

        if(!ushort.TryParse(_port, out ushort port))
        {
            DisplayError("Invalid port");
            return Error.InvalidParameter;
        }

        Error err = _client.ConnectToHost(ip, port);
        if(err != Error.Ok)
        {
            DisplayError($"Connecting to server failed with error: {err}");
        }
        return err;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            CloseConnection();
        }
    }

    #region Signal Handling

    /// <summary>
    /// Data received
    /// </summary>
    /// <param name="packetBytes">The bytes</param>
    private void OnWebSocketClientPacketReceived(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        //add to buffer
        _buffer.PushRightRange(packetBytes);
        //create packets and handle them
        while(_buffer.Count > 0 && AbstractPacket.TryConstructPacketFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(packet);
        }
    }

    /// <summary>
    /// Connected to server
    /// </summary>
    private void OnWebSocketClientConnected()
    {
        EmitSignal(SignalName.Connected);
    }

    /// <summary>
    /// Connection closed
    /// </summary>
    private void OnWebSocketClientConnectionClosed()
    {
        _lobby = null;
        _lobbyConnectionPacket = null;
        _placePacket = null;
        _refillSent = false;
        _quitSent = false;
        _buffer.Clear();
        EmitSignal(SignalName.Disconnected);
    }

    #endregion

    #region Packet Handling

    /// <summary>
    /// Send a packet
    /// </summary>
    /// <param name="packet">The packet to send</param>
    private void SendPacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if(_client.State != WebSocketPeer.State.Open)
        {
            DisplayError("Connection to server is not yet established. Please wait.");
            return;
        }

        Error err = _client.SendPacket(packet.ToByteArray());
        if(err != Error.Ok)
            DisplayError($"Error {err} while trying to communicate with server");
    }

    /// <summary>
    /// Handle a packet
    /// </summary>
    /// <param name="packet">The packet to handle</param>
    private void HandlePacket(AbstractPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        //giant switch statement to handle all packets
        //can't use polymorphism since that would require to expose the GameClient privates to the packets.
        //and can't use a dictionary since we need to cast the packets (and doing so generically would require reflection)
        switch(packet)
        {
            case Packet_Dummy _packet:
                HandlePacket_Dummy(_packet);
                break;
            case Packet_InvalidPacket _packet:
                HandlePacket_InvalidPacket(_packet);
                break;
            case Packet_InvalidPacketInform _packet:
                HandlePacket_InvalidPacketInform(_packet);
                break;
            case Packet_CreateLobbyOk _packet:
                HandlePacket_CreateLobbyOk(_packet);
                break;
            case Packet_CreateLobbyFail _packet:
                HandlePacket_CreateLobbyFail(_packet);
                break;
            case Packet_ConnectLobbyOk _packet:
                HandlePacket_ConnectLobbyOk(_packet);
                break;
            case Packet_ConnectLobbyFail _packet:
                HandlePacket_ConnectLobbyFail(_packet);
                break;
            case Packet_LobbyNewPlayer _packet:
                HandlePacket_LobbyNewPlayer(_packet);
                break;
            case Packet_NewGameRequestOk _packet:
                HandlePacket_NewGameRequestOk(_packet);
                break;
            case Packet_NewGameRequestFail _packet:
                HandlePacket_NewGameRequestFail(_packet);
                break;
            case Packet_NewGameRequested _packet:
                HandlePacket_NewGameRequested(_packet);
                break;
            case Packet_NewGameAcceptOk _packet:
                HandlePacket_NewGameAcceptOk(_packet);
                break;
            case Packet_NewGameAcceptFail _packet:
                HandlePacket_NewGameAcceptFail(_packet);
                break;
            case Packet_NewGameAccepted _packet:
                HandlePacket_NewGameAccepted(_packet);
                break;
            case Packet_NewGameRejectOk _packet:
                HandlePacket_NewGameRejectOk(_packet);
                break;
            case Packet_NewGameRejectFail _packet:
                HandlePacket_NewGameRejectFail(_packet);
                break;
            case Packet_NewGameRejected _packet:
                HandlePacket_NewGameRejected(_packet);
                break;
            case Packet_NewGameCancelOk _packet:
                HandlePacket_NewGameCancelOk(_packet);
                break;
            case Packet_NewGameCancelFail _packet:
                HandlePacket_NewGameCancelFail(_packet);
                break;
            case Packet_NewGameCanceled _packet:
                HandlePacket_NewGameCanceled(_packet);
                break;
            case Packet_LobbyPlayerBusyTrue _packet:
                HandlePacket_LobbyPlayerBusyTrue(_packet);
                break;
            case Packet_LobbyPlayerBusyFalse _packet:
                HandlePacket_LobbyPlayerBusyFalse(_packet);
                break;
            case Packet_LobbyDisconnectOther _packet:
                HandlePacket_LobbyDisconnectOther(_packet);
                break;
            case Packet_LobbyTimeoutWarning _packet:
                HandlePacket_LobbyTimeoutWarning(_packet);
                break;
            case Packet_LobbyTimeout _packet:
                HandlePacket_LobbyTimeout(_packet);
                break;
            case Packet_NewGameStarting _packet:
                HandlePacket_NewGameStarting(_packet);
                break;
            case Packet_GameActionPlaceOk _packet:
                HandlePacket_GameActionPlaceOk(_packet);
                break;
            case Packet_GameActionPlaceFail _packet:
                HandlePacket_GameActionPlaceFail(_packet);
                break;
            case Packet_GameActionPlaceOther _packet:
                HandlePacket_GameActionPlaceOther(_packet);
                break;
            case Packet_GameActionRefillOk _packet:
                HandlePacket_GameActionRefillOk(_packet);
                break;
            case Packet_GameActionRefillFail _packet:
                HandlePacket_GameActionRefillFail(_packet);
                break;
            case Packet_GameActionRefillOther _packet:
                HandlePacket_GameActionRefillOther(_packet);
                break;
            case Packet_GameQuitOk _packet:
                HandlePacket_GameQuitOk(_packet);
                break;
            case Packet_GameQuitFail _packet:
                HandlePacket_GameQuitFail(_packet);
                break;
            case Packet_GameQuitOther _packet:
                HandlePacket_GameQuitOther(_packet);
                break;
            case Packet_GameFinished _packet:
                HandlePacket_GameFinished(_packet);
                break;
            case Packet_ServerClosing _packet:
                HandlePacket_ServerClosing(_packet);
                break;
            default:
                GD.Print($"Client did not expect to get packet of type {packet.GetType().Name}");
                Desync();
                break;
        }
    }

    /// <summary>
    /// Handle dummy packet
    /// </summary>
    /// <param name="packet">The packet</param>
    private static void HandlePacket_Dummy(Packet_Dummy packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Got dummy packet");
    }

    /// <summary>
    /// Handle an invalid packet
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_InvalidPacket(Packet_InvalidPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got invalid packet: {packet.GivenPacketType}");
        DisplayError("Bad packet from server");
        Desync();
    }

    /// <summary>
    /// Handle the server informing of an invalid packet
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_InvalidPacketInform(Packet_InvalidPacketInform packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server informed about invalid packet: {packet.GivenPacketType}");
        DisplayError("Something went wrong while communicating with the server");
        Desync();
    }

    /// <summary>
    /// Handle server approving lobby creation
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_CreateLobbyOk(Packet_CreateLobbyOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server created lobby: {packet.LobbyId}");
        if(_lobby is not null)
        {
            GD.Print("But I am already in a lobby??");
            Desync();
            return;
        }
        if(_lobbyConnectionPacket is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }

        //create lobby object
        _lobby = new()
        {
            Id = packet.LobbyId,
            Players = new List<Player>()
            {
                new()
                {
                    Name = ClientName,
                    Index = 0
                }
            },
            Index = 0
        };

        EmitSignal(SignalName.LobbyEntered, packet.LobbyId, new LobbyPlayerData[]{new(ClientName, false)}, _lobby.Index);
    }

    /// <summary>
    /// Handle lobby creation failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_CreateLobbyFail(Packet_CreateLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Creating lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionPacket is not null)
        {
            GD.Print("But I didn't request that??");
            Desync();
            return;
        }
        _lobbyConnectionPacket = null;
        DisplayError($"Creating lobby failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
    }

    /// <summary>
    /// Handle connecting to lobby
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_ConnectLobbyOk(Packet_ConnectLobbyOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.Players);
        GD.Print($"Connected to lobby! Other players: {string.Join(' ', packet.Players.Select(p => p.Name))}");
        if(_lobbyConnectionPacket is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }

        //create lobby object
        int i = 0;
        _lobby = new()
        {
            Id = _lobbyConnectionPacket.LobbyId,
            Players = packet.Players.Select(player => new Player()
            {
                Name = player.Name,
                Busy = player.Busy,
                Index = i++
            }).ToList(),
            Index = packet.YourIndex
        };

        EmitSignal(SignalName.LobbyEntered, _lobbyConnectionPacket.LobbyId, packet.Players, _lobby.Index);
        _lobbyConnectionPacket = null;
    }

    /// <summary>
    /// Handle lobby connection failure
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_ConnectLobbyFail(Packet_ConnectLobbyFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Connecting to lobby failed with error: {packet.ErrorCode}");
        if(_lobbyConnectionPacket is null)
        {
            GD.Print("But I didn't ask to connect??");
            Desync();
            return;
        }

        DisplayError($"Connecting to lobby failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");

        _lobby = null;
        _lobbyConnectionPacket = null;
    }

    /// <summary>
    /// Handle new player joining the lobby
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyNewPlayer(Packet_LobbyNewPlayer packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.PlayerName);
        GD.Print($"New player joined lobby: {packet.PlayerName}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        _lobby.Players.Add(new()
        {
            Name = packet.PlayerName,
            Index = _lobby.Players.Count
        });

        EmitSignal(SignalName.LobbyPlayerJoined, packet.PlayerName);
    }

    /// <summary>
    /// Handle game request being sent succesfully
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRequestOk(Packet_NewGameRequestOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Sending game request to {packet.RequestTargetIndex} succeeded");
        int targetIdx = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(targetIdx < 0 || _lobby.Players.Count <= targetIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player target = _lobby.Players[targetIdx];
        if(!target.GameRequestSent)
        {
            GD.Print("But I didn't request??");
            Desync();
            return;
        }
        target.IMadeRequest = true;
        EmitSignal(SignalName.NewGameRequestSent, targetIdx);
        target.GameRequestSent = false;
    }

    /// <summary>
    /// Handle game request failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRequestFail(Packet_NewGameRequestFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Sending new game request to {packet.RequestTargetIndex} failed with error: {packet.ErrorCode}");
        int targetIdx = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(targetIdx < 0 || _lobby.Players.Count <= targetIdx)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        if(targetIdx == _lobby.Index)
        {
                GD.Print("But that would be me??");
                Desync();
                return;
        }

        Player other = _lobby.Players[targetIdx];
        if(!other.GameRequestSent)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        other.IMadeRequest = false;
        //due to timing we might send the game request before we receive the other player's
        //if that happens we move on and don't display an error
        if(!other.IGotRequest)
            DisplayError($"Sending game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameRequestSent = false;
    }

    /// <summary>
    /// Handle receiving game request
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRequested(Packet_NewGameRequested packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got game request from {packet.RequestSourceIndex}");
        int sourceIdx = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(sourceIdx < 0 || _lobby.Players.Count <= sourceIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player source = _lobby.Players[sourceIdx];
        if(source.IMadeRequest || source.IGotRequest)
        {
            GD.Print("But there's already a request??");
            Desync();
            return;
        }
        source.IGotRequest = true;
        EmitSignal(SignalName.NewGameRequestReceived, sourceIdx);
    }

    /// <summary>
    /// Handle game request accept being succesful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameAcceptOk(Packet_NewGameAcceptOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Approving player {packet.RequestSourceIndex}'s request succeded");
        int sourceIdx = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(sourceIdx < 0 || _lobby.Players.Count <= sourceIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player source = _lobby.Players[sourceIdx];
        if(!source.GameAcceptSent)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        source.IGotRequest = false;
        source.IMadeRequest = false;
        _lobby.Opponent = source;
        EmitSignal(SignalName.NewGameAcceptSent, sourceIdx);
        source.GameAcceptSent = false;
    }

    /// <summary>
    /// Handle game request accept failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameAcceptFail(Packet_NewGameAcceptFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Accepting a game request from {packet.RequestSourceIndex} failed with error: {packet.ErrorCode}");
        int sourceIdx = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(sourceIdx < 0 || _lobby.Players.Count <= sourceIdx)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        if(sourceIdx == _lobby.Index)
        {
            GD.Print("But that would be me??");
            Desync();
            return;
        }

        Player other = _lobby.Players[sourceIdx];
        if(!other.GameAcceptSent)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Accepting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameAcceptSent = false;
    }

    /// <summary>
    /// Handle game request being accepted
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameAccepted(Packet_NewGameAccepted packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.RequestTargetIndex} accepted game request");
        int targetIdx = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(targetIdx < 0 || _lobby.Players.Count <= targetIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player target = _lobby.Players[targetIdx];
        if(!target.IMadeRequest)
        {
            GD.Print("But I didn't request??");
            Desync();
            return;
        }
        target.IMadeRequest = false;
        _lobby.Opponent = target;
        EmitSignal(SignalName.NewGameAcceptReceived, targetIdx);
    }

    /// <summary>
    /// Handle game request reject being succesful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRejectOk(Packet_NewGameRejectOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Rejecting player {packet.RequestSourceIndex}'s request succeeded");
        int sourceIdx = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(sourceIdx < 0 || _lobby.Players.Count <= sourceIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player source = _lobby.Players[sourceIdx];
        if(!source.GameRejectSent)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        source.IGotRequest = false;
        EmitSignal(SignalName.NewGameRejectSent, sourceIdx);
        source.GameRejectSent = false;
    }

    /// <summary>
    /// Handle game request reject failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameRejectFail(Packet_NewGameRejectFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Rejecting a game request from {packet.RequestSourceIndex} failed with error: {packet.ErrorCode}");
        int index = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(index < 0 || _lobby.Players.Count <= index)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        if(index == _lobby.Index)
        {
            GD.Print("But that would be me??");
            Desync();
            return;
        }

        Player other = _lobby.Players[index];
        if(!other.GameRejectSent)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Rejecting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameRejectSent = false;
    }

    /// <summary>
    /// Handle game request being rejected
    /// </summary>
    /// <param name="packet">The paket</param>
    private void HandlePacket_NewGameRejected(Packet_NewGameRejected packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.RequestTargetIndex} rejected game request");
        int targetIdx = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(targetIdx < 0 || _lobby.Players.Count <= targetIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player target = _lobby.Players[targetIdx];
        if(!target.IMadeRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        target.IMadeRequest = false;
        EmitSignal(SignalName.NewGameRejectReceived, targetIdx);
    }

    /// <summary>
    /// Handle game request cancel being succseful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameCancelOk(Packet_NewGameCancelOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Canceling game request to {packet.RequestTargetIndex} succeeded");
        int targetIdx = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(targetIdx < 0 || _lobby.Players.Count <= targetIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player target = _lobby.Players[targetIdx];
        if(!target.GameCancelSent)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        target.IMadeRequest = false;
        EmitSignal(SignalName.NewGameCancelSent, targetIdx);
        target.GameCancelSent = false;
    }

    /// <summary>
    /// Handle game request cancel failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameCancelFail(Packet_NewGameCancelFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Canceling a new game request failed with error: {packet.ErrorCode}");
        int index = packet.RequestTargetIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(index < 0 || _lobby.Players.Count <= index)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        if(index == _lobby.Index)
        {
            GD.Print("But that would be me??");
            Desync();
            return;
        }

        Player other = _lobby.Players[index];
        if(!other.GameCancelSent)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        DisplayError($"Canceling game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameCancelSent = false;
    }

    /// <summary>
    /// Handle game request being canceled
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameCanceled(Packet_NewGameCanceled packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.RequestSourceIndex} canceled game request");
        int sourceIdx = packet.RequestSourceIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(sourceIdx < 0 || _lobby.Players.Count <= sourceIdx)
        {
            GD.Print("But that player is invalid??");
            Desync();
            return;
        }

        Player source = _lobby.Players[sourceIdx];
        if(!source.IGotRequest)
        {
            GD.Print("But there's no request??");
            Desync();
            return;
        }
        source.IGotRequest = false;
        EmitSignal(SignalName.NewGameCancelReceived, sourceIdx);
    }

    /// <summary>
    /// Handle player going into a game
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyPlayerBusyTrue(Packet_LobbyPlayerBusyTrue packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.PlayerIndex} became busy");
        int index = packet.PlayerIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(index < 0 || _lobby.Players.Count <= index)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        //me
        if(index == _lobby.Index)
        {
            return;
        }

        Player other = _lobby.Players[index];
        other.Busy = true;
        ResetPlayerData(other);
        EmitSignal(SignalName.PlayerBecameBusy, index);
    }

    /// <summary>
    /// Handle player leaving a game
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyPlayerBusyFalse(Packet_LobbyPlayerBusyFalse packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.PlayerIndex} no longer busy");
        int index = packet.PlayerIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(index < 0 || _lobby.Players.Count <= index)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        //me
        if(index == _lobby.Index)
        {
            return;
        }

        Player other = _lobby.Players[index];
        other.Busy = false;
        EmitSignal(SignalName.PlayerBecameAvailable, index);
    }

    /// <summary>
    /// Handle player disconnecting from lobby
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyDisconnectOther(Packet_LobbyDisconnectOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Player {packet.PlayerIndex} disconnected: {packet.Reason}");
        int index = packet.PlayerIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(index < 0 || _lobby.Players.Count <= index)
        {
            GD.Print("But that index is invalid??");
            Desync();
            return;
        }

        if(index == _lobby.Index)
        {
            GD.Print("But that would be me??");
            Desync();
            return;
        }

        Player other = _lobby.Players[index];

        if(other == _lobby.Opponent)
        {
            _lobby.Opponent = null;
            EmitSignal(SignalName.GameQuitByOpponent);
        }

        //he is before me
        if(index < _lobby.Index)
        {
            _lobby.Index--;
        }
        _lobby.Players.RemoveAt(index);
        //updated indices
        for(int i = 0; i < _lobby.Players.Count; ++i)
        {
            _lobby.Players[i].Index = i;
        }

        EmitSignal(SignalName.LobbyPlayerLeft, index);
    }

    /// <summary>
    /// Handle lobby timeout warning
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyTimeoutWarning(Packet_LobbyTimeoutWarning packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Lobby will timeout in {packet.SecondsRemaining}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        EmitSignal(SignalName.LobbyTimeoutWarned, packet.SecondsRemaining);
    }

    /// <summary>
    /// Handle lobby timeout
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_LobbyTimeout(Packet_LobbyTimeout packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Lobby timed out");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        EmitSignal(SignalName.LobbyTimedOut);
        _lobby = null;
        _lobbyConnectionPacket = null;
    }

    /// <summary>
    /// Handle new game starting
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_NewGameStarting(Packet_NewGameStarting packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"New game is starting! Turn: {packet.Turn}. Opponent: {packet.OpponentIndex}");
        int opponentIdx = packet.OpponentIndex;
        if(_lobby is null)
        {
            GD.Print("But I'm not in a lobby??");
            Desync();
            return;
        }

        if(opponentIdx < 0 || _lobby.Players.Count <= opponentIdx)
        {
            GD.Print("But that opponent is invalid??");
            Desync();
            return;
        }

        Player opponent = _lobby.Players[opponentIdx];
        if(_lobby.Opponent is null)
        {
            GD.Print("But I was not aware of that??");
            Desync();
            return;
        }
        if(opponent != _lobby.Opponent)
        {
            GD.Print("But that is the wrong person??");
            Desync();
            return;
        }

        for(int i = 0; i < _lobby.Players.Count; ++i)
        {
            if(i == _lobby.Index || i == opponentIdx) continue;
            ResetPlayerData(_lobby.Players[i]);
        }

        EmitSignal(SignalName.GameStarted, (int)packet.Turn, opponentIdx);
    }

    /// <summary>
    /// Handle token placing being succesful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionPlaceOk(Packet_GameActionPlaceOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Placing was succesful");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        if(_placePacket is null)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        if(!ResourceLoader.Exists(_placePacket.ScenePath))
        {
            GD.Print("Server approved place action but the scene does not exist??");
            Desync();
            return;
        }
        Resource res = ResourceLoader.Load(_placePacket.ScenePath);
        if(res is not PackedScene scene)
        {
            GD.Print("Server approved place action but the resource is not a scene??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionPlaceSent, _placePacket.Column, scene);
        _placePacket = null;
    }

    /// <summary>
    /// Handle token placing failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionPlaceFail(Packet_GameActionPlaceFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Placing failed with error: {packet.ErrorCode}");
        if(_placePacket is null)
        {
            GD.Print("But I didn't send a place request??");
            Desync();
            return;
        }
        DisplayError($"Placing token failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _placePacket = null;
    }

    /// <summary>
    /// Handle opponent placing token
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionPlaceOther(Packet_GameActionPlaceOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.ScenePath);
        GD.Print($"Other player is placing token at {packet.Column}. Token type: {packet.ScenePath}");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        if(!ResourceLoader.Exists(packet.ScenePath))
        {
            GD.Print("Other player sent game action place with nonexistent scene path??");
            Desync();
            return;
        }
        Resource res = ResourceLoader.Load(packet.ScenePath);
        if(res is not PackedScene scene)
        {
            GD.Print("Other player sent game action place with path that points to a non scene resource??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionPlaceReceived, packet.Column, scene);
    }

    /// <summary>
    /// Handle refill being succesful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionRefillOk(Packet_GameActionRefillOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Refill was succesful");
        if(!_refillSent)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionRefillSent);
        _refillSent = false;
    }

    /// <summary>
    /// Handle refill failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionRefillFail(Packet_GameActionRefillFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Refilling failed with error: {packet.ErrorCode}");
        if(!_refillSent)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        DisplayError($"Refill failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _refillSent = false;
    }
    
    /// <summary>
    /// Handle opponent refilling
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameActionRefillOther(Packet_GameActionRefillOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Other player is refilling");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionRefillReceived);
    }

    /// <summary>
    /// Handle game quit being succesful
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameQuitOk(Packet_GameQuitOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Quit ok");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        if(!_quitSent)
        {
            GD.Print("But I didn't ask to quit??");
            Desync();
            return;
        }
        _lobby.Opponent = null;
        EmitSignal(SignalName.GameQuitBySelf);
        _quitSent = false;
    }

    /// <summary>
    /// Handle game quit failing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameQuitFail(Packet_GameQuitFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Quit failed with error {packet.ErrorCode}");
        if(!_quitSent)
        {
            GD.Print("But I didn't ask to quit??");
            Desync();
            return;
        }
        DisplayError($"Quitting failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _quitSent = false;
    }

    /// <summary>
    /// Handle opponent quitting the game
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameQuitOther(Packet_GameQuitOther packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Other player is quitting");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        _lobby.Opponent = null;
        EmitSignal(SignalName.GameQuitByOpponent);
    }

    /// <summary>
    /// Handle game finishing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_GameFinished(Packet_GameFinished packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Game finished! Result: {packet.Result}. Player 1 score: {packet.Player1Score}. Player 2 score: {packet.Player2Score}");
        if(_lobby is null || _lobby.Opponent is null)
        {
            GD.Print("But I'm not in a game??");
            Desync();
            return;
        }
        _lobby.Opponent = null;
        EmitSignal(SignalName.GameFinished);
    }

    /// <summary>
    /// Handle server closing
    /// </summary>
    /// <param name="packet">The packet</param>
    private void HandlePacket_ServerClosing(Packet_ServerClosing packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Server closing!");
        CloseConnection();
        EmitSignal(SignalName.ServerClosed);
    }

    #endregion

    #region Operations

    //This section is for functions called by the UI

    /// <summary>
    /// Create a lobby
    /// </summary>
    public void CreateLobby()
    {
        if(_lobby is not null) return;
        SendPacket(new Packet_CreateLobbyRequest(ClientName));
    }

    /// <summary>
    /// Join a lobby
    /// </summary>
    /// <param name="lobby">The lobby id</param>
    public void JoinLobby(uint lobby)
    {
        if(_lobby is not null) return;
        _lobbyConnectionPacket = new Packet_ConnectLobbyRequest(lobby, ClientName);
        SendPacket(_lobbyConnectionPacket);
    }

    /// <summary>
    /// Disconnect from the lobby
    /// </summary>
    /// <param name="reason">The reason for disconnecting</param>
    public void DisconnectFromLobby(DisconnectReasonEnum reason)
    {
        if(_lobby is null) return;
        SendPacket(new Packet_LobbyDisconnect(reason));
        _lobby = null;
        _lobbyConnectionPacket = null;
    }

    /// <summary>
    /// Disconnect from the server
    /// </summary>
    /// <param name="reason">The reason for disconnecting</param>
    public void DisconnectFromServer(DisconnectReasonEnum reason)
    {
        DisconnectFromLobby(reason);
        CloseConnection();
    }

    /// <summary>
    /// Send a new game request
    /// </summary>
    /// <param name="index">The player index</param>
    public void RequestNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index || index == _lobby.Index) return;
        Player p = _lobby.Players[index];
        if(p.Busy || p.IMadeRequest || p.IGotRequest || p.GameRequestSent) return;
        p.GameRequestSent = true;
        SendPacket(new Packet_NewGameRequest(index));
    }

    /// <summary>
    /// Accept a game request
    /// </summary>
    /// <param name="index">The player index</param>
    public void AcceptNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.IGotRequest || p.GameAcceptSent) return;
        p.GameAcceptSent = true;
        SendPacket(new Packet_NewGameAccept(index));
    }

    /// <summary>
    /// Reject a game request
    /// </summary>
    /// <param name="index">The player index</param>
    public void RejectNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.IGotRequest || p.GameRejectSent) return;
        p.GameRejectSent = true;
        SendPacket(new Packet_NewGameReject(index));
    }

    /// <summary>
    /// Cancel a game request
    /// </summary>
    /// <param name="index">The player index</param>
    public void CancelNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.IMadeRequest || p.GameCancelSent) return;
        p.GameCancelSent = true;
        SendPacket(new Packet_NewGameCancel(index));
    }

    /// <summary>
    /// Place a token
    /// </summary>
    /// <param name="column">The column to place in</param>
    /// <param name="path">The scene path of the token to place</param>
    public void PlaceToken(byte column, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(_lobby is null || _lobby.Opponent is null || _placePacket is not null) return;
        _placePacket = new Packet_GameActionPlace(column, path);
        SendPacket(_placePacket);
    }

    /// <summary>
    /// Refill
    /// </summary>
    public void Refill()
    {
        if(_lobby is null || _lobby.Opponent is null || _refillSent) return;
        _refillSent = true;
        SendPacket(new Packet_GameActionRefill());
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        if(_lobby is null || _lobby.Opponent is null || _quitSent) return;
        _quitSent = true;
        SendPacket(new Packet_GameQuit());
    }

    /// <summary>
    /// Used for when the server responses do not make sense with the current client-side state.
    /// Displays an error and disconnects
    /// </summary>
    public void Desync()
    {
        GD.PushError("Desync detected");
        DisplayError("Something went wrong while communicating with the server");
        DisconnectFromServer(DisconnectReasonEnum.DESYNC);
    }

    #endregion

    /// <summary>
    /// Close the connection to the server
    /// </summary>
    public void CloseConnection()
    {
        _client.Close();
        _lobby = null;
        _lobbyConnectionPacket = null;
        _placePacket = null;
        _refillSent = false;
        _quitSent = false;
    }

    /// <summary>
    /// Clear request data for the player
    /// </summary>
    /// <param name="player">The player</param>
    private static void ResetPlayerData(Player player)
    {
        player.IMadeRequest = false;
        player.IGotRequest = false;
        player.GameRequestSent = false;
        player.GameAcceptSent = false;
        player.GameRejectSent = false;
        player.GameCancelSent = false;
    }

    /// <summary>
    /// Display an error in the ui
    /// </summary>
    /// <param name="error">The error string to display</param>
    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        EmitSignal(SignalName.ErrorOccured, error);
    }
}