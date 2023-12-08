using System;

namespace FourInARowBattle;

public static class Autoloads
{
    private static T GetAutoload<T>(T? _t) => _t ?? throw new InvalidOperationException($"Cannot grab {typeof(T).Name} autoload as it is null. Make sure that it is initialized");
    private static void SetAutoload<T>(ref T? _t, T value)
    {
        if(_t is not null)
            throw new InvalidOperationException($"Attempt to set {typeof(T).Name} autoload when it is already set");
        _t = value;
    }

    private static EventBus? _eventBus = null;
    public static EventBus EventBus{get => GetAutoload(_eventBus); set => SetAutoload(ref _eventBus, value);}
    
    private static PersistentData? _persData = null;
    public static PersistentData PersistentData{get => GetAutoload(_persData); set => SetAutoload(ref _persData, value);}
    
    private static ObjectPool? _objectPool = null;
    public static ObjectPool ObjectPool{get => GetAutoload(_objectPool); set => SetAutoload(ref _objectPool, value);}

    private static GlobalResources? _globalRes = null;
    public static GlobalResources GlobalResources{get => GetAutoload(_globalRes); set => SetAutoload(ref _globalRes, value);}

    private static AudioManager? _audioManage = null;
    public static AudioManager AudioManager{get => GetAutoload(_audioManage); set => SetAutoload(ref _audioManage, value);}
}
