using System;
using System.Collections.Generic;
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
    private string[] _handshakeHeaders = Array.Empty<string>();
    [Export]
    private string[] _supportedProtocols = Array.Empty<string>();
    [Export]
    private bool _useWSS = false;

    private TlsOptions? _tlsOptions = TlsOptions.ClientUnsafe();

    private WebSocketPeer _socket = new();
    private WebSocketPeer.State _lastState = WebSocketPeer.State.Closed;

    public WebSocketPeer.State State => _socket.GetReadyState();

    public Error ConnectToHost(string ip, ushort port)
    {
        ArgumentNullException.ThrowIfNull(ip);

        string url = $"{(_useWSS ? "wss" : "ws")}://{ip}:{port}";

        _socket.HandshakeHeaders = _handshakeHeaders;
        _socket.SupportedProtocols = _supportedProtocols;
        Error err = _socket.ConnectToUrl(url, _tlsOptions);
        if(err != Error.Ok)
        {
            GD.PushError($"Error {err} while attempting to connect to url {url}");
            return err;
        }
        _lastState = _socket.GetReadyState();
        return Error.Ok;
    }

    public Error SendPacket(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return _socket.PutPacket(packet);
    }

    public byte[]? GetPacket()
    {
        if(_socket.GetAvailablePacketCount() < 1) return null;
        return _socket.GetPacket();
    }

    public Error TryGetPacket(out byte[]? packet)
    {
        packet = null;
        if(_socket.GetAvailablePacketCount() < 1) return Error.Ok;
        packet = _socket.GetPacket();
        return _socket.GetPacketError();
    }

    public void Close(int code = 1000, string reason = "")
    {
        _socket.Close(code, reason ?? "");
    }

    public void Clear()
    {
        _socket = new();
        _lastState = _socket.GetReadyState();
    }

    public void Poll()
    {
        WebSocketPeer.State state = _socket.GetReadyState();
        if(state != WebSocketPeer.State.Closed)
            _socket.Poll();
        if(_lastState != state)
        {
            _lastState = state;
            if(state == WebSocketPeer.State.Open)
                EmitSignal(SignalName.ConnectedToServer);
            else if(state == WebSocketPeer.State.Closed)
                EmitSignal(SignalName.ConnectionClosed);
        }
        
        List<byte> batchedPackets = new();
        while(_socket.GetReadyState() == WebSocketPeer.State.Open && _socket.GetAvailablePacketCount() > 0)
        {
            Error err = TryGetPacket(out byte[]? packet);
            if(err != Error.Ok)
            {
                GD.PushError($"Error {err} while trying to get packet");
                break;
            }
            if(packet is not null)
                batchedPackets.AddRange(packet);
        }
        EmitSignal(SignalName.PacketReceived, batchedPackets.ToArray());
    }

    public override void _Process(double delta)
    {
        Poll();
    }
}