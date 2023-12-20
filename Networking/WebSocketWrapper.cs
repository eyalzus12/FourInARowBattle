using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FourInARowBattle;

public partial class WebSocketWrapper : Node
{
    public enum AutoconnectModeEnum
    {
        //will not automatically connect. default.
        NONE,
        //will connect when the node is ready.
        SELF_READY,
        //will connect when the parent is ready.
        PARENT_READY,
        //will connect when the owner is ready.
        OWNER_READY,
        //will connect when the root is ready.
        ROOT_READY,
        //will connect when a specified node is ready.
        CUSTOM,
    }

    [Signal]
    public delegate void ConnectedEventHandler(string url);
    [Signal]
    public delegate void ConnectFailedEventHandler();
    [Signal]
    public delegate void ReceivedEventHandler(byte[] data);
    [Signal]
    public delegate void ClosingEventHandler();
    [Signal]
    public delegate void ClosedEventHandler(int code, string reason);

    [Export(PropertyHint.Range, "0,128,or_greater")]
    public int ReceiveLimit{get; set;} = 0;
    [Export(PropertyHint.Range, "0,300,or_greater")]
    public int ConnectionTimeout{get; set;} = 10;

    [ExportGroup("Routing")]
    [Export]
    public string Host{get; set;} = "127.0.0.1";
    [Export]
    public string Route{get; set;} = "/";
    [Export]
    public bool UseWSS{get; set;} = true;

    [ExportGroup("Autoconnect")]
    [Export(PropertyHint.Enum)]
    public AutoconnectModeEnum AutoconnectMode{get; set;} = AutoconnectModeEnum.NONE;
    [Export]
    public Node? AutoconnectReference{get; set;} = null;

    public WebSocketPeer Socket{get; private set;} = new();
    public List<byte> Buffer{get; private set;} = new();
    public byte[]? LastSent{get; private set;}
    public byte[]? LastReceived{get; private set;}
    public int RecievedCount{get; private set;} = 0;
    public Timer ConnectTimer{get; private set;} = new(){OneShot = true};
    private int _RC = 0;

    private string _fullURL = null!;

    public WebSocketPeer.State SocketState{get
    {
        Socket.Poll();
        return Socket.GetReadyState();
    }}

    public bool ConnectTimedOut => ConnectTimer.IsStopped() && ConnectionTimeout > 0;

    public bool SocketConnected{get; private set;} = false;
    public bool ClosingStarted{get; private set;} = false;

    //async void is generally bad practice, but that doesn't matter here
    public override async void _Ready()
    {
        AddChild(ConnectTimer);

        if(AutoconnectMode != AutoconnectModeEnum.NONE)
        {
            switch(AutoconnectMode)
            {
                case AutoconnectModeEnum.PARENT_READY:
                    Node? par = GetParent();
                    if(par is not null)
                        await ToSignal(par, Node.SignalName.Ready);
                    break;
                case AutoconnectModeEnum.OWNER_READY:
                    if(Owner is not null)
                        await ToSignal(Owner, Node.SignalName.Ready);
                    break;
                case AutoconnectModeEnum.ROOT_READY:
                    await ToSignal(GetTree().Root, Node.SignalName.Ready);
                    break;
                case AutoconnectModeEnum.CUSTOM:
                    if(AutoconnectReference is not null)
                    {
                        Node arBind = AutoconnectReference;
                        if(arBind.GetParent() is null)
                            await ToSignal(arBind, Node.SignalName.Ready);
                    }
                    break;
                case AutoconnectModeEnum.SELF_READY:
                    break;
                default:
                    GD.PushError($"Unknown autoconnect mode {AutoconnectMode}");
                    break;
            }

            ConnectSocket(Host, Route);
        }
    }

    public override void _Process(double delta)
    {
        Socket.Poll();

        switch(SocketState)
        {
            //socket connecting
            case WebSocketPeer.State.Connecting:
                //timeout
                if(ConnectTimedOut)
                {
                    Socket.Close(1001, "Connection timeout");
                    EmitSignal(WebSocketWrapper.SignalName.ConnectFailed);
                }
                break;
            //socket open
            case WebSocketPeer.State.Open:
                //stop timeout timer
                if(!SocketConnected)
                {
                    SocketConnected = true;
                    ClosingStarted = false;
                    ConnectTimer.Stop();
                    EmitSignal(WebSocketWrapper.SignalName.Connected, _fullURL);
                }

                //read all available packets
                int available = Socket.GetAvailablePacketCount();
                bool enableRecieve = ReceiveLimit == 0 || _RC < ReceiveLimit;
                if(available > 0 && enableRecieve)
                {
                    Buffer.AddRange(Socket.GetPacket());
                    _RC++;
                }
                //ran out of packets. emit signal.
                else if(Buffer.Count > 0)
                {
                    LastReceived = Buffer.ToArray();
                    RecievedCount = _RC;
                    EmitSignal(WebSocketWrapper.SignalName.Received, LastReceived);
                    _RC = 0;
                    Buffer.Clear();
                }
                break;
            //closing
            case WebSocketPeer.State.Closing:
                if(!ClosingStarted)
                {
                    EmitSignal(WebSocketWrapper.SignalName.Closing);
                }
                break;
            //closed
            case WebSocketPeer.State.Closed:
                int code = Socket.GetCloseCode();
                string reason = Socket.GetCloseReason();
                EmitSignal(WebSocketWrapper.SignalName.Closed, code, reason);
                SetProcess(false);
                break;
            default:
                GD.PushError($"Unknown socket state {SocketState}");
                break;
        }
    }

    public bool ConnectSocket(string host, string route)
    {
        if(SocketConnected)
        {
            GD.PushError("Cannot connect a socket already in use");
            return false;
        }

        ConnectTimer.Start(ConnectionTimeout);
        SetProcess(true);

        Host = host;
        string protocol = UseWSS ? "wss" : "ws";
        _fullURL = $"{protocol}://{host}/{route.TrimPrefix("/")}";

        Error err = Socket.ConnectToUrl(_fullURL);
        if(err != Error.Ok)
        {
            GD.PushError($"Error {err} while trying to connect to socket on {_fullURL}");
            return false;
        }

        return true;
    }

    //read all available packets from the socket
    public byte[]? Receive()
    {
        if(!CheckOpen()) return null;

        Buffer.Clear();
        while(Socket.GetAvailablePacketCount() != 0)
        {
            Buffer.AddRange(Socket.GetPacket());
        }
        return Buffer.ToArray();
    }

    //send data through the socket
    public void Send(byte[] packet)
    {
        if(!CheckOpen()) return;

        LastSent = packet.ToArray();
        Socket.PutPacket(packet);
    }

    public bool CheckOpen()
    {
        if(!SocketConnected)
        {
            GD.PushError("Socket not connected yet");
            return false;
        }

        if(ClosingStarted)
        {
            GD.PushError("Socket is closed or closing");
            return false;
        }

        return true;
    }
}
