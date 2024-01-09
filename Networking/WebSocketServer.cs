using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace FourInARowBattle;

public partial class WebSocketServer : Node
{
    private sealed class PendingPeer
    {
        public ulong ConnectTime{get; set;}
        public StreamPeerTcp Tcp{get; set;}
        public StreamPeer Connection{get; set;}
        public WebSocketPeer? WebSocket{get; set;}

        public PendingPeer(StreamPeerTcp tcp)
        {
            ArgumentNullException.ThrowIfNull(tcp);
            Tcp = tcp;
            Connection = tcp;
            ConnectTime = Time.GetTicksMsec();
        }
    }

    [Signal]
    public delegate void ClientConnectedEventHandler(int peerId);
    [Signal]
    public delegate void ClientDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void PacketReceivedEventHandler(int peerId, byte[] packet);

    [Export]
    public string[] HandshakeHeaders{get; set;} = Array.Empty<string>();
    [Export]
    public string[] SupportedProtocols{get; set;} = Array.Empty<string>();
    [Export]
    public ulong HandshakeTimeout{get; set;} = 3000;

    private bool _refuseNewConnections = false;
    [Export]
    public bool RefuseNewConnections{get => _refuseNewConnections; set
    {
        _refuseNewConnections = value;
        if(_refuseNewConnections)
            _pendingPeers.Clear();
    }}

    [ExportCategory("Tls")]
    [Export]
    public bool UseTls{get; set;} = false;
    [Export]
    public X509Certificate? TlsCert{get; set;} = null;
    [Export]
    public CryptoKey? TlsKey{get; set;} = null;

    private readonly TcpServer _tcpServer = new();
    private readonly HashSet<PendingPeer> _pendingPeers = new();
    private readonly Dictionary<int, WebSocketPeer> _peers = new();

    public Error Listen(ushort port)
    {
        if(_tcpServer.IsListening())
        {
            GD.PushError("Attempt to listen on already listening TCP server");
            return Error.AlreadyInUse;
        }
        return _tcpServer.Listen(port);
    }

    public void Stop()
    {
        _tcpServer.Stop();
        _pendingPeers.Clear();
        _peers.Clear();
    }

    public Error SendPacket(int peerId, byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        // peerId > 0 -> Send one
        // peerId == 0 -> Send all (Broadcast)
        // peerId < 0 -> Send all excluding one
        if(peerId < 0)
        {
            Error? firstErr = null;
            foreach((int id, WebSocketPeer ws) in _peers)
            {
                if(id == -peerId) continue;
                Error err = ws.PutPacket(packet);
                if(err != Error.Ok)
                {
                    GD.PushError($"Error while sending packet: {err}");
                    firstErr ??= err;
                }
            }
            return firstErr ?? Error.Ok;
        }
        else
        {
            if(!_peers.TryGetValue(peerId, out WebSocketPeer? ws))
            {
                GD.PushError($"Peer Id {peerId} does not exist");
                return Error.DoesNotExist;
            }
            return ws.PutPacket(packet);
        }
    }

    public byte[]? GetPacket(int peerId)
    {
        if(!_peers.TryGetValue(peerId, out WebSocketPeer? ws))
        {
            GD.PushError($"Peer Id {peerId} does not exist");
            return null;
        }
        if(ws.GetAvailablePacketCount() < 1)
            return null;
        return ws.GetPacket();
    }

    public Error TryGetPacket(int peerId, out byte[]? packet)
    {
        packet = null;
        if(!_peers.TryGetValue(peerId, out WebSocketPeer? ws))
        {
            GD.PushError($"Peer Id {peerId} does not exist");
            return Error.DoesNotExist;
        }
        if(ws.GetAvailablePacketCount() < 1)
            return Error.Ok;
        packet = ws.GetPacket();
        return ws.GetPacketError();
    }

    public bool HasPacket(int peerId)
    {
        if(!_peers.TryGetValue(peerId, out WebSocketPeer? ws))
        {
            GD.PushError($"Peer Id {peerId} does not exist");
            return false;
        }
        return ws.GetAvailablePacketCount() > 0;
    }

    private WebSocketPeer CreatePeer() => new()
    {
        SupportedProtocols = SupportedProtocols,
        HandshakeHeaders = HandshakeHeaders
    };

    public void Poll()
    {
        if(!_tcpServer.IsListening())
            return;
        
        //get new pending peers
        while(!RefuseNewConnections && _tcpServer.IsConnectionAvailable())
        {
            StreamPeerTcp? sp = _tcpServer.TakeConnection();
            if(sp is null)
            {
                GD.PushError("Got null stream peer while taking a tcp connection");
                continue;
            }
            _pendingPeers.Add(new(sp));
        }

        //timeout peers
        List<PendingPeer> pendingPeersCopy = _pendingPeers.ToList();
        foreach(PendingPeer peer in pendingPeersCopy)
        {
            if(ConnectPending(peer) || peer.ConnectTime + HandshakeTimeout < Time.GetTicksMsec())
            {
                _pendingPeers.Remove(peer);
            }
        }

        //get packets and disconnect peers that deserve it
        List<KeyValuePair<int, WebSocketPeer>> peersCopy = _peers.ToList();
        foreach((int id, WebSocketPeer ws) in peersCopy)
        {
            ws.Poll();
            if(ws.GetReadyState() != WebSocketPeer.State.Open)
            {
                EmitSignal(SignalName.ClientDisconnected, id);
                _peers.Remove(id);
                continue;
            }
            while(ws.GetAvailablePacketCount() > 0)
            {
                Error err = TryGetPacket(id, out byte[]? packet);
                if(err != Error.Ok)
                {
                    GD.PushError($"Error {err} while trying to read packet from {id}");
                    break;
                }
                if(packet is not null)
                    EmitSignal(SignalName.PacketReceived, packet);
            }
        }
    }

    private bool ConnectPending(PendingPeer peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        if(peer.WebSocket is not null)
        {
            peer.WebSocket.Poll();
            WebSocketPeer.State state = peer.WebSocket.GetReadyState();
            if(state == WebSocketPeer.State.Open)
            {
                //find unused id
                int id; do{id = GD.RandRange(2, 1 << 30);} while(_peers.ContainsKey(id));
                _peers.Add(id, peer.WebSocket);
                EmitSignal(SignalName.ClientConnected, id);
                return true; // Success
            }
            else if(state == WebSocketPeer.State.Connecting)
            {
                return false; // Still connecting
            }
            else
            {
                return true; // Failure
            }
        }
        else if(peer.Tcp.GetStatus() != StreamPeerTcp.Status.Connected)
        {
            return true; // Tcp Disconnected
        }
        else if(!UseTls)
        {
            // Tcp is ready. Create WebSocket peer.
            peer.WebSocket = CreatePeer();
            peer.WebSocket.AcceptStream(peer.Tcp);
            return false; // WebSocketPeer connection is pending
        }
        else
        {
            if(peer.Connection == peer.Tcp)
            {
                if(TlsKey is null || TlsCert is null)
                {
                    GD.PushError("Attempt to use Tls while Tls key and cert are null");
                    return true;
                }
                StreamPeerTls tls = new();
                tls.AcceptStream(peer.Tcp, TlsOptions.Server(TlsKey, TlsCert));
                peer.Connection = tls;
            }
            if(peer.Connection is not StreamPeerTls tlsConn)
            {
                GD.PushError("Logical error. Trying to do tls connection but connection is not a StreamPeerTls");
                return true;
            }
            StreamPeerTls.Status status = tlsConn.GetStatus();
            if(status == StreamPeerTls.Status.Connected)
            {
                peer.WebSocket = CreatePeer();
                peer.WebSocket.AcceptStream(peer.Connection);
                return false; // WebSocketPeer connection is pending
            }
            else if(status == StreamPeerTls.Status.Handshaking)
            {
                return false; // Handshaking
            }
            else
            {
                return true; // Failure
            }
        }
    }

    public override void _Process(double delta)
    {
        Poll();
    }
}