using Godot;
using System;
using DequeNet;

namespace FourInARowBattle;

//this class wraps the WebSocketWrapper (a WebSocketWrapperWrapper, if you will)
//and sends out a signal when there's a full packet in the buffer
public partial class PacketConverter : Node
{
    [Signal]
    public delegate void GotPacketEventHandler(AbstractPacket packet);

    [Export]
    public WebSocketWrapper? Socket{get; set;} = null;

    public Deque<byte> Buffer{get; private set;} = new();

    public override void _Ready()
    {
        if(Socket is null) return;
        Socket.Received += OnSocketReceived;
    }

    public void OnSocketReceived(byte[] data)
    {
        foreach(byte b in data)
            Buffer.PushRight(b);
        if(AbstractPacket.TryConstructFrom(Buffer, out AbstractPacket? packet))
        {
            EmitSignal(PacketConverter.SignalName.GotPacket, packet);
        }
    }

    
}
