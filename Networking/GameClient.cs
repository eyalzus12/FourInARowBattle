using Godot;
using DequeNet;

namespace FourInARowBattle;

public partial class GameClient : Node
{
    [Export]
    public WebSocketClient? Client{get; set;}

    private readonly Deque<byte> _buffer = new();

    public override void _Ready()
    {
        if(Client is not null)
            Client.PacketReceived += OnWebSocketClientPacketReceived;
    }

    public void OnWebSocketClientPacketReceived(byte[] packetBytes)
    {
        foreach(byte b in packetBytes) _buffer.PushRight(b);
        if(_buffer.Count > 0)
        {
            if(AbstractPacket.TryConstructFrom(_buffer, out AbstractPacket? packet))
            {
                HandlePacket(packet);
            }
        }
    }
    public void SendPacket(AbstractPacket packet)
    {
        Client?.SendPacket(packet.ToByteArray());
    }

    public void HandlePacket(AbstractPacket packet)
    {
        switch(packet)
        {
            case Packet_Dummy:
            {
                GD.Print("Got dummy packet");
                break;
            }
            case Packet_InvalidPacket _packet:
            {
                GD.Print($"Got invalid packet: {_packet.GivenPacketType}");
                break;
            }
            case Packet_InvalidPacketInform _packet:
            {
                GD.Print($"Server informed about invalid packet: {_packet.GivenPacketType}");
                break;
            }
            case Packet_CreateLobbyRequest:
            {
                GD.PushError("I'm client. Why did I get create lobby request?");
                break;
            }
            case Packet_CreateLobbyOk _packet:
            {
                GD.Print($"Server created lobby: {_packet.LobbyId}");
                break;
            }
            case Packet_CreateLobbyFail _packet:
            {
                GD.Print($"Creating lobby failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_ConnectLobbyRequest:
            {
                GD.PushError("I'm client. Why did I get connect lobby request?");
                break;
            }
            case Packet_ConnectLobbyOk _packet:
            {
                GD.Print($"Connected to lobby! Other player: {_packet.OtherPlayerName}");
                break;
            }
            case Packet_ConnectLobbyFail _packet:
            {
                GD.Print($"Connecting to lobby failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_LobbyNewPlayer _packet:
            {
                GD.Print($"New player joined lobby: {_packet.OtherPlayerName}");
                break;
            }
            case Packet_NewGameRequest:
            {
                GD.PushError("I'm client. Why did I get new game request?");
                break;
            }
            case Packet_NewGameRequestOk:
            {
                GD.Print("Sending new game request was succesful");
                break;
            }
            case Packet_NewGameRequestFail _packet:
            {
                GD.Print($"Sending new game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameRequested:
            {
                GD.Print("Other player wants to start a game");
                break;
            }
            case Packet_NewGameAccept:
            {
                GD.PushError("I'm client. Why did I get new game accept?");
                break;
            }
            case Packet_NewGameAcceptOk:
            {
                GD.Print("Accepting new game request was succesful");
                break;
            }
            case Packet_NewGameAcceptFail _packet:
            {
                GD.Print($"Accepting a new game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameAccepted:
            {
                GD.Print("New game request was accepted!");
                break;
            }
            case Packet_NewGameReject:
            {
                GD.PushError("I'm client. Why did I get new game reject?");
                break;
            }
            case Packet_NewGameRejectOk:
            {
                GD.PushError("Rejecting new game request was succesful");
                break;
            }
            case Packet_NewGameRejectFail _packet:
            {
                GD.Print($"Rejecting a new game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameRejected:
            {
                GD.Print("New game request was rejected :(");
                break;
            }
            case Packet_NewGameCancel:
            {
                GD.PushError("I'm client. Why did I get new game cancel?");
                break;
            }
            case Packet_NewGameCancelOk:
            {
                GD.Print("Canceling a new game request was succesful");
                break;
            }
            case Packet_NewGameCancelFail _packet:
            {
                GD.Print($"Canceling a new game request failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_NewGameCanceled:
            {
                GD.Print("New game request was canceled");
                break;
            }
            case Packet_LobbyDisconnect:
            {
                GD.PushError("I'm client. Why did I get lobby disconnect?");
                break;
            }
            case Packet_LobbyDisconnectOther _packet:
            {
                GD.Print($"Other player disconnected: {_packet.Reason}");
                break;
            }
            case Packet_LobbyTimeoutWarning _packet:
            {
                GD.Print($"Lobby will timeout in {_packet.SecondsRemaining}");
                break;
            }
            case Packet_LobbyTimeout:
            {
                GD.Print("Lobby timed out");
                break;
            }
            case Packet_NewGameStarting _packet:
            {
                GD.Print($"New game is starting! My color: {_packet.GameTurn}");
                break;
            }
            case Packet_GameActionPlace:
            {
                GD.PushError("I'm client. Why did I get game action place?");
                break;
            }
            case Packet_GameActionPlaceOk:
            {
                GD.Print("Placing was succesful");
                break;
            }
            case Packet_GameActionPlaceFail _packet:
            {
                GD.Print($"Placing failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_GameActionPlaceOther _packet:
            {
                GD.Print($"Other player is placing token at {_packet.Column}. Token type: {_packet.ScenePath}");
                break;
            }
            case Packet_GameActionRefill:
            {
                GD.PushError("I'm client. Why did I get game action refill?");
                break;
            }
            case Packet_GameActionRefillOk:
            {
                GD.Print("Refill was succesful");
                break;
            }
            case Packet_GameActionRefillFail _packet:
            {
                GD.Print($"Refilling failed with error: {_packet.ErrorCode}");
                break;
            }
            case Packet_GameActionRefillOther:
            {
                GD.Print("Other player is refilling");
                break;
            }
            case Packet_GameFinished _packet:
            {
                GD.Print($"Game finished! Result: {_packet.Result}. Player 1 score: {_packet.Player1Score}. Player 2 score: {_packet.Player2Score}");
                break;
            }
            default:
            {
                GD.PushError($"Unknown packet type {packet.GetType().Name}");
                break;
            }
        }
    }
}