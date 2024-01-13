using Godot;
using DequeNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

public partial class GameClient : Node
{
    #region Signals

    [Signal]
    public delegate void ConnectedEventHandler();
    [Signal]
    public delegate void DisconnectedEventHandler();
    [Signal]
    public delegate void ServerClosedEventHandler();
    [Signal]
    public delegate void ErrorOccuredEventHandler(string description);
    [Signal]
    public delegate void LobbyEnteredEventHandler(uint lobbyId, LobbyPlayerData[] players, int index);
    [Signal]
    public delegate void LobbyPlayerLeftEventHandler(int index);
    [Signal]
    public delegate void LobbyPlayerJoinedEventHandler(string name);
    [Signal]
    public delegate void LobbyTimeoutWarnedEventHandler(int secondsRemaining);
    [Signal]
    public delegate void LobbyTimedOutEventHandler();
    [Signal]
    public delegate void GameQuitByOpponentEventHandler();
    [Signal]
    public delegate void GameQuitBySelfEventHandler();
    [Signal]
    public delegate void NewGameRequestSentEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameRequestReceivedEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameAcceptSentEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameAcceptReceivedEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameRejectSentEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameRejectReceivedEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameCancelSentEventHandler(int playerIndex);
    [Signal]
    public delegate void NewGameCancelReceivedEventHandler(int playerIndex);
    [Signal]
    public delegate void PlayerBecameBusyEventHandler(int playerIndex);
    [Signal]
    public delegate void PlayerBecameAvailableEventHandler(int playerIndex);
    [Signal]
    public delegate void GameStartedEventHandler(GameTurnEnum turn, int opponentIndex);
    [Signal]
    public delegate void GameActionPlaceSentEventHandler(int column, PackedScene scene);
    [Signal]
    public delegate void GameActionPlaceReceivedEventHandler(int column, PackedScene scene);
    [Signal]
    public delegate void GameActionRefillSentEventHandler();
    [Signal]
    public delegate void GameActionRefillReceivedEventHandler();
    [Signal]
    public delegate void GameFinishedEventHandler();

    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private WebSocketClient _client = null!;

    private readonly Deque<byte> _buffer = new();

    public string ClientName{get; set;} = "";

    #region State Variables

    private sealed class Player
    {
        public string Name{get; set;} = "";
        public int Index{get; set;}
        public bool ISentRequest{get; set;} = false;
        public bool IGotRequest{get; set;} = false;
        public bool Busy{get; set;} = false;
        public Packet_NewGameRequest? GameRequestPacket{get; set;}
        public Packet_NewGameAccept? GameAcceptPacket{get; set;}
        public Packet_NewGameReject? GameRejectPacket{get; set;}
        public Packet_NewGameCancel? GameCancelPacket{get; set;}
    }

    private sealed class Lobby
    {
        public uint Id{get; set;}
        public List<Player> Players{get; set;} = new();
        public Player? Opponent{get; set;} = null;
        public int Index{get; set;}
    }

    private Lobby? _lobby = null;

    private Packet_ConnectLobbyRequest? _lobbyConnectionPacket = null;

    private Packet_GameActionPlace? _placePacket = null;
    private Packet_GameActionRefill? _refillPacket = null;
    private Packet_GameQuit? _quitPacket = null;

    #endregion

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_client);
    }

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

    public void ConnectToServer(string ip, string _port)
    {
        ArgumentNullException.ThrowIfNull(ip);
        ArgumentNullException.ThrowIfNull(_port);
        
        if(!ushort.TryParse(_port, out ushort port))
        {
            DisplayError("Invalid port");
            return;
        }

        Error err = _client.ConnectToUrl($"ws://{ip}:{port}");
        if(err != Error.Ok)
        {
            DisplayError($"Connecting to server failed with error: {err}");
        }
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
        {
            CloseConnection();
        }
    }

    #region Signal Handling

    private void OnWebSocketClientPacketReceived(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        _buffer.PushRightRange(packetBytes);

        while(_buffer.Count > 0 && AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
        {
            HandlePacket(packet);
        }
    }

    private void OnWebSocketClientConnected()
    {
        EmitSignal(SignalName.Connected);
    }

    private void OnWebSocketClientConnectionClosed()
    {
        _lobby = null;
        _lobbyConnectionPacket = null;
        _placePacket = null;
        _refillPacket = null;
        _quitPacket = null;
        EmitSignal(SignalName.Disconnected);
    }

    #endregion

    #region Packet Handling

    public void SendPacket(AbstractPacket packet)
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

    public void HandlePacket(AbstractPacket packet)
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

    private void HandlePacket_Dummy(Packet_Dummy packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Got dummy packet");
    }

    private void HandlePacket_InvalidPacket(Packet_InvalidPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Got invalid packet: {packet.GivenPacketType}");
        DisplayError("Bad packet from server");
        Desync();
    }

    private void HandlePacket_InvalidPacketInform(Packet_InvalidPacketInform packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Server informed about invalid packet: {packet.GivenPacketType}");
        DisplayError("Something went wrong while communicating with the server");
        Desync();
    }

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

        _lobby = new()
        {
            Id = packet.LobbyId,
            Players = new List<Player>()
            {
                new()
                {
                    Name = ClientName!,
                    Index = 0
                }
            },
            Index = 0
        };

        EmitSignal(SignalName.LobbyEntered, packet.LobbyId, new LobbyPlayerData[]{new(ClientName!, false)}, _lobby.Index);
    }

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

    private void HandlePacket_LobbyNewPlayer(Packet_LobbyNewPlayer packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.OtherPlayerName);
        GD.Print($"New player joined lobby: {packet.OtherPlayerName}");
        if(_lobby is null)
        {
            GD.Print("But I am not in a lobby??");
            Desync();
            return;
        }
        _lobby.Players.Add(new()
        {
            Name = packet.OtherPlayerName,
            Index = _lobby.Players.Count
        });

        EmitSignal(SignalName.LobbyPlayerJoined, packet.OtherPlayerName);
    }

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
        if(target.GameRequestPacket is null)
        {
            GD.Print("But I didn't request??");
            Desync();
            return;
        }
        target.ISentRequest = true;
        EmitSignal(SignalName.NewGameRequestSent, targetIdx);
        target.GameRequestPacket = null;
    }

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
        if(other.GameRequestPacket is null)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        other.ISentRequest = false;
        //due to timing we might send the game request before we receive the other player's
        //if that happens we move on and don't display an error
        if(!other.IGotRequest)
            DisplayError($"Sending game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameRequestPacket = null;
    }


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
        if(source.ISentRequest || source.IGotRequest)
        {
            GD.Print("But there's already a request??");
            Desync();
            return;
        }
        source.IGotRequest = true;
        EmitSignal(SignalName.NewGameRequestReceived, sourceIdx);
    }

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
        if(source.GameAcceptPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        source.IGotRequest = false;
        source.ISentRequest = false;
        _lobby.Opponent = source;
        EmitSignal(SignalName.NewGameAcceptSent, sourceIdx);
        source.GameAcceptPacket = null;
    }

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
        if(other.GameAcceptPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Accepting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameAcceptPacket = null;
    }

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
        if(!target.ISentRequest)
        {
            GD.Print("But I didn't request??");
            Desync();
            return;
        }
        target.ISentRequest = false;
        _lobby.Opponent = target;
        EmitSignal(SignalName.NewGameAcceptReceived, targetIdx);
    }

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
        if(source.GameRejectPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        source.IGotRequest = false;
        EmitSignal(SignalName.NewGameRejectSent, sourceIdx);
        source.GameRejectPacket = null;
    }

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
        if(other.GameRejectPacket is null)
        {
            GD.Print("But I didn't answer??");
            Desync();
            return;
        }
        DisplayError($"Rejecting game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameRejectPacket = null;
    }

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
        if(!target.ISentRequest)
        {
            GD.Print("But I don't have a request??");
            Desync();
            return;
        }
        target.ISentRequest = false;
        EmitSignal(SignalName.NewGameRejectReceived, targetIdx);
    }

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
        if(target.GameCancelPacket is null)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        target.ISentRequest = false;
        EmitSignal(SignalName.NewGameCancelSent, targetIdx);
        target.GameCancelPacket = null;
    }

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
        if(other.GameCancelPacket is null)
        {
            GD.Print("But I didn't cancel??");
            Desync();
            return;
        }
        DisplayError($"Canceling game request failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        other.GameCancelPacket = null;
    }

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
        EmitSignal(SignalName.PlayerBecameBusy, index);
    }

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
        for(int i = 0; i < _lobby.Players.Count; ++i)
        {
            _lobby.Players[i].Index = i;
        }

        EmitSignal(SignalName.LobbyPlayerLeft, index);
    }
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
            Player another = _lobby.Players[i];
            another.ISentRequest = false;
            another.IGotRequest = false;
            another.GameRequestPacket = null;
            another.GameAcceptPacket = null;
            another.GameRejectPacket = null;
            another.GameCancelPacket = null;
        }

        EmitSignal(SignalName.GameStarted, (int)packet.Turn, opponentIdx);
    }

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

    private void HandlePacket_GameActionRefillOk(Packet_GameActionRefillOk packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Refill was succesful");
        if(_refillPacket is null)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        EmitSignal(SignalName.GameActionRefillSent);
        _refillPacket = null;
    }

    private void HandlePacket_GameActionRefillFail(Packet_GameActionRefillFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Refilling failed with error: {packet.ErrorCode}");
        if(_refillPacket is null)
        {
            GD.Print("But I didn't send a refill??");
            Desync();
            return;
        }
        DisplayError($"Refill failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _refillPacket = null;
    }

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
        if(_quitPacket is null)
        {
            GD.Print("But I didn't ask to quit??");
            Desync();
            return;
        }
        _lobby.Opponent = null;
        EmitSignal(SignalName.GameQuitBySelf);
        _quitPacket = null;
    }

    private void HandlePacket_GameQuitFail(Packet_GameQuitFail packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print($"Quit failed with error {packet.ErrorCode}");
        if(_quitPacket is null)
        {
            GD.Print("But I didn't ask to quit??");
            Desync();
            return;
        }
        DisplayError($"Quitting failed with error: {ErrorCodeUtils.Humanize(packet.ErrorCode)}");
        _quitPacket = null;
    }

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

    private void HandlePacket_ServerClosing(Packet_ServerClosing packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        GD.Print("Server closing!");
        CloseConnection();
        EmitSignal(SignalName.ServerClosed);
    }

    #endregion

    #region Operations

    public void CreateLobby()
    {
        if(_lobby is not null) return;
        SendPacket(new Packet_CreateLobbyRequest(ClientName));
    }

    public void JoinLobby(uint lobby)
    {
        if(_lobby is not null) return;
        _lobbyConnectionPacket = new Packet_ConnectLobbyRequest(lobby, ClientName);
        SendPacket(_lobbyConnectionPacket);
    }

    public void DisconnectFromLobby(DisconnectReasonEnum reason)
    {
        if(_lobby is null) return;
        SendPacket(new Packet_LobbyDisconnect(reason));
        _lobby = null;
        _lobbyConnectionPacket = null;
    }

    public void DisconnectFromServer(DisconnectReasonEnum reason)
    {
        DisconnectFromLobby(reason);
        CloseConnection();
    }

    public void RequestNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index || index == _lobby.Index) return;
        Player p = _lobby.Players[index];
        if(p.Busy || p.ISentRequest || p.IGotRequest || p.GameRequestPacket is not null) return;
        p.GameRequestPacket = new Packet_NewGameRequest(index);
        SendPacket(p.GameRequestPacket);
    }

    public void AcceptNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.IGotRequest || p.GameAcceptPacket is not null) return;
        p.GameAcceptPacket = new Packet_NewGameAccept(index);
        SendPacket(p.GameAcceptPacket);
    }
    
    public void RejectNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.IGotRequest || p.GameRejectPacket is not null) return;
        p.GameRejectPacket = new Packet_NewGameReject(index);
        SendPacket(p.GameRejectPacket);
    }

    public void CancelNewGame(int index)
    {
        if(_lobby is null || _lobby.Opponent is not null) return;
        if(index < 0 || _lobby.Players.Count <= index) return;
        Player p = _lobby.Players[index];
        if(!p.ISentRequest || p.GameCancelPacket is not null) return;
        p.GameCancelPacket = new Packet_NewGameCancel(index);
        SendPacket(p.GameCancelPacket);
    }
    
    public void PlaceToken(byte column, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(_lobby is null || _lobby.Opponent is null || _placePacket is not null) return;
        _placePacket = new Packet_GameActionPlace(column, path);
        SendPacket(_placePacket);
    }

    public void Refill()
    {
        if(_lobby is null || _lobby.Opponent is null || _refillPacket is not null) return;
        _refillPacket = new Packet_GameActionRefill();
        SendPacket(_refillPacket);
    }

    public void QuitGame()
    {
        if(_lobby is null || _lobby.Opponent is null || _quitPacket is not null) return;
        _quitPacket = new Packet_GameQuit();
        SendPacket(_quitPacket);
    }

    public void Desync()
    {
        GD.PushError("Desync detected");
        DisplayError("Something went wrong while communicating with the server");
        DisconnectFromServer(DisconnectReasonEnum.DESYNC);
    }

    #endregion

    public void CloseConnection()
    {
        _client.Close();
        _lobby = null;
        _lobbyConnectionPacket = null;
        _placePacket = null;
        _refillPacket = null;
        _quitPacket = null;
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        EmitSignal(SignalName.ErrorOccured, error);
    }
}