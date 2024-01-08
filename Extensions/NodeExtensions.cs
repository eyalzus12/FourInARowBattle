using Godot;

namespace FourInARowBattle;

public static class NodeExtensions
{
    public static void QueueFreeDeferred(this Node n) => n.CallDeferred(Node.MethodName.QueueFree);
    public static void RemoveChildDeferred(this Node n, Node child) => n.CallDeferred(Node.MethodName.RemoveChild, child);
}