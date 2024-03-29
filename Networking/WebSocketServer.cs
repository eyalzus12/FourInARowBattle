using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// TCP server wrapper over websocket
/// </summary>
public partial class WebSocketServer : Node
{
    public const int MIN_PEER_ID = 2;
    public const int MAX_PEER_ID = 1 << 30;

    /// <summary>
    /// Internal class for storing connection-pending peers.
    /// </summary>
    private sealed class PendingPeer
    {
        /// <summary>
        /// The time that connection started in msec
        /// </summary>
        public ulong ConnectTime{get; set;}
        /// <summary>
        /// The tcp stream
        /// </summary>
        public StreamPeerTcp Tcp{get; set;}
        /// <summary>
        /// The connection stream
        /// </summary>
        public StreamPeer Connection{get; set;}
        /// <summary>
        /// The websocket
        /// </summary>
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
    private string[] _handshakeHeaders = Array.Empty<string>();
    [Export]
    private string[] _supportedProtocols = Array.Empty<string>();
    [Export]
    private ulong _handshakeTimeout = 3000;

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
    private bool _useTls = false;
    [Export]
    private X509Certificate? _tlsCert = null;
    [Export]
    private CryptoKey? _tlsKey = null;

    private readonly TcpServer _tcpServer = new();

    public bool Listening => _tcpServer.IsListening();

    private readonly HashSet<PendingPeer> _pendingPeers = new();
    private readonly Dictionary<int, WebSocketPeer> _peers = new();

    /// <summary>
    /// Start listening on port
    /// </summary>
    /// <param name="port">The port to listen on</param>
    public Error Listen(ushort port)
    {
        if(_tcpServer.IsListening())
        {
            GD.PushError("Attempt to listen on already listening TCP server");
            return Error.AlreadyInUse;
        }
        return _tcpServer.Listen(port);
    }

    /// <summary>
    /// Stop server
    /// </summary>
    public void Stop()
    {
        _tcpServer.Stop();
        _pendingPeers.Clear();
        _peers.Clear();
    }

    /// <summary>
    /// Send a packet
    /// </summary>
    /// <param name="peerId">The id to send to</param>
    /// <param name="packet">The packet to send</param>
    public Error SendPacket(int peerId, byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        // peerId > 0 -> Send one
        // peerId == 0 -> Send all (Broadcast)
        // peerId < 0 -> Send all excluding one
        if(peerId <= 0)
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

    /// <summary>
    /// Get a raw packet, or null if none exist
    /// </summary>
    /// <param name="peerId">The id to read from</param>
    /// <returns>The raw packet</returns>
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

    /// <summary>
    /// Get a raw packet, or the error
    /// </summary>
    /// <param name="peerId">The id to read from</param>
    /// <param name="packet">The raw packet</param>
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

    /// <summary>
    /// Check if id has packets waiting
    /// </summary>
    /// <param name="peerId">The id</param>
    /// <returns>Whether there are waiting packets</returns>
    public bool HasPacket(int peerId)
    {
        if(!_peers.TryGetValue(peerId, out WebSocketPeer? ws))
        {
            GD.PushError($"Peer Id {peerId} does not exist");
            return false;
        }
        return ws.GetAvailablePacketCount() > 0;
    }

    /// <summary>
    /// Create a new websocket
    /// </summary>
    /// <returns>The new websocket</returns>
    private WebSocketPeer CreatePeer() => new()
    {
        SupportedProtocols = _supportedProtocols,
        HandshakeHeaders = _handshakeHeaders
    };

    /// <summary>
    /// Update state and receive connections
    /// </summary>
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
            if(ConnectPending(peer) || peer.ConnectTime + _handshakeTimeout < Time.GetTicksMsec())
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
                {
                    EmitSignal(SignalName.PacketReceived, id, packet);
                }
            }
        }
    }

    /// <summary>
    /// Check if peer has finished connecting (this includes failure)
    /// </summary>
    /// <param name="peer">The peer</param>
    /// <returns>Whether the connection finished</returns>
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
                int id; do{id = GD.RandRange(MIN_PEER_ID, MAX_PEER_ID);} while(_peers.ContainsKey(id));
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
        else if(!_useTls)
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
                if(_tlsKey is null || _tlsCert is null)
                {
                    GD.PushError("Attempt to use Tls while Tls key and cert are null");
                    return true;
                }
                StreamPeerTls tls = new();
                tls.AcceptStream(peer.Tcp, TlsOptions.Server(_tlsKey, _tlsCert));
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