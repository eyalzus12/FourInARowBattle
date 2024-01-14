using System;
using Godot;

namespace FourInARowBattle;

public static class Autoloads
{
    private static T GetAutoload<T>(T? _t)
    {
        if(_t is null)
        {
            GD.PushError($"Cannot grab {typeof(T).Name} autoload as it is null. Make sure that it is initialized");
            return default!;
        }
        return _t;
    }
    private static void SetAutoload<T>(ref T? _t, T value)
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
    public static Startup Startup{get => GetAutoload(_startup); set => SetAutoload(ref _startup, value);}

    private static EventBus? _eventBus = null;
    public static EventBus EventBus{get => GetAutoload(_eventBus); set => SetAutoload(ref _eventBus, value);}
    
    private static PersistentData? _persData = null;
    public static PersistentData PersistentData{get => GetAutoload(_persData); set => SetAutoload(ref _persData, value);}
    
    private static ScenePool? _objectPool = null;
    public static ScenePool ScenePool{get => GetAutoload(_objectPool); set => SetAutoload(ref _objectPool, value);}

    private static GlobalResources? _globalRes = null;
    public static GlobalResources GlobalResources{get => GetAutoload(_globalRes); set => SetAutoload(ref _globalRes, value);}

    private static AudioManager? _audioManage = null;
    public static AudioManager AudioManager{get => GetAutoload(_audioManage); set => SetAutoload(ref _audioManage, value);}
}
