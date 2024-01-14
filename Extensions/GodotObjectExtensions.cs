using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// GodotObject extensions
/// </summary>
public static class GodotObjectExtensions
{
    /// <summary>
    /// A nullable-aware wrapper over GodotObject.IsInstanceValid, that also checks IsQueuedForDeletion.
    /// </summary>
    /// <param name="o">The GodotObject to check</param>
    /// <returns>Whether the instance is valid</returns>
    public static bool IsInstanceValid([NotNullWhen(true)] this GodotObject? o) =>
        o is not null && GodotObject.IsInstanceValid(o) && !o.IsQueuedForDeletion();
    
    /// <summary>
    /// Connect a signal to a callable if it is not already connected
    /// </summary>
    /// <param name="o">The object to connect to</param>
    /// <param name="signal">The signal to connec to</param>
    /// <param name="callable">The callable to connect</param>
    /// <param name="flags">The connection flags</param>
    public static void ConnectIfNotConnected(this GodotObject o, StringName signal, Callable callable, GodotObject.ConnectFlags flags = 0)
    {
        if(!o.IsConnected(signal, callable))
            o.Connect(signal, callable, (uint)flags);
    }
}