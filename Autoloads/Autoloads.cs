using System;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// Class to make grabbing autoloads easier
/// </summary>
public static class Autoloads
{
    /// <summary>
    /// Helper function. Return given autoload or error if it is null.
    /// </summary>
    /// <param name="_t">The autoload</param>
    /// <typeparam name="T">The type of autoload</typeparam>
    private static T GetAutoload<T>(T? _t) where T : Node
    {
        if(_t is null)
        {
            GD.PushError($"Cannot grab {typeof(T).Name} autoload as it is null. Make sure that it is initialized");
            return null!;
        }
        return _t;
    }
    /// <summary>
    /// Helper function. Set given autoload to value, or error if it already has a value.
    /// </summary>
    /// <param name="_t">The autoload to set</param>
    /// <param name="value">The value to set it to</param>
    /// <typeparam name="T">The type of autoload</typeparam>
    private static void SetAutoload<T>(ref T? _t, T value) where T : Node
    {
        if(value is null)
        {
            GD.PushError($"Attempt to set {typeof(T).Name} autoload to null");
            return;
        }

        if(_t is not null)
        {
            GD.PushError($"Attempt to set {typeof(T).Name} autoload when it is already set");
            return;
        }
        
        _t = value;
    }

    private static Startup? _startup = null;
    /// <summary>
    /// Startup autoload
    /// </summary>
    public static Startup Startup{get => GetAutoload(_startup); set => SetAutoload(ref _startup, value);}
    
    private static PersistentData? _persData = null;
    /// <summary>
    /// PersistentData autoload
    /// </summary>
    public static PersistentData PersistentData{get => GetAutoload(_persData); set => SetAutoload(ref _persData, value);}
    
    private static ScenePool? _objectPool = null;
    /// <summary>
    /// ScenePool autoload
    /// </summary>
    public static ScenePool ScenePool{get => GetAutoload(_objectPool); set => SetAutoload(ref _objectPool, value);}

    private static GlobalResources? _globalRes = null;
    /// <summary>
    /// GlobalResources autoload
    /// </summary>
    public static GlobalResources GlobalResources{get => GetAutoload(_globalRes); set => SetAutoload(ref _globalRes, value);}

    private static AudioManager? _audioManage = null;
    /// <summary>
    /// AudioManager autoload
    /// </summary>
    public static AudioManager AudioManager{get => GetAutoload(_audioManage); set => SetAutoload(ref _audioManage, value);}
}
