using System;
using Godot;

namespace FourInARowBattle;

public partial class WebSocketClient : Node
{
    [Signal]
    public delegate void ConnectedToServerEventHandler();
    [Signal]
    public delegate void ConnectionClosedEventHandler();
    [Signal]
    public delegate void PacketReceivedEventHandler(byte[] packet);

    [Export]
    public string[] HandshakeHeaders{get; set;} = Array.Empty<string>();
    [Export]
    public string[] SupportedProtocols{get; set;} = Array.Empty<string>();

    public TlsOptions? TlsOptions{get; set;} = null;
    public WebSocketPeer Socket{get; set;} = new();
    public WebSocketPeer.State LastState{get; set;} = WebSocketPeer.State.Closed;

    public Error ConnectToUrl(string url)
    {
        Socket.HandshakeHeaders = HandshakeHeaders;
        Socket.SupportedProtocols = SupportedProtocols;
        Error err = Socket.ConnectToUrl(url, TlsOptions);
        if(err != Error.Ok)
        {
            GD.PushError($"Error {err} while attempting to connect to url {url}");
            return err;
        }
        LastState = Socket.GetReadyState();
        return Error.Ok;
    }

    public Error SendPacket(byte[] packet)
    {
        return Socket.PutPacket(packet);
    }

    public byte[]? GetPacket()
    {
        if(Socket.GetAvailablePacketCount() < 1) return null;
        return Socket.GetPacket();
    }

    public Error TryGetPacket(out byte[]? packet)
    {
        packet = null;
        if(Socket.GetAvailablePacketCount() < 1) return Error.Ok;
        packet = Socket.GetPacket();
        return Socket.GetPacketError();
    }

    public void Close(int code = 1000, string reason = "")
    {
        Socket.Close(code, reason);
    }

    public void Clear()
    {
        Socket = new();
        LastState = Socket.GetReadyState();
    }

    public void Poll()
    {
        WebSocketPeer.State state = Socket.GetReadyState();
        if(state != WebSocketPeer.State.Closed)
            Socket.Poll();
        if(LastState != state)
        {
            LastState = state;
            if(state == WebSocketPeer.State.Open)
                EmitSignal(WebSocketClient.SignalName.ConnectedToServer);
            else if(state == WebSocketPeer.State.Closed)
                EmitSignal(WebSocketClient.SignalName.ConnectionClosed);
        }
        while(Socket.GetReadyState() == WebSocketPeer.State.Open && Socket.GetAvailablePacketCount() > 0)
        {
            Error err = TryGetPacket(out byte[]? packet);
            if(err != Error.Ok)
            {
                GD.PushError($"Error {err} while trying to get packet");
                break;
            }
            if(packet is not null)
                EmitSignal(WebSocketClient.SignalName.PacketReceived, packet);
        }
    }

    public override void _Process(double delta)
    {
        Poll();
    }
}