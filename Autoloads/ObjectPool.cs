using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class ObjectPool : Node
{
    //how many new objects to create if pool is empty
    public const int OBJECT_POOL_FACTOR = 3;

    private readonly Dictionary<PackedScene, Queue<Node>> _objectPoolDict = new();
    private readonly Dictionary<PackedScene, Queue<Node>> _objectPoolReturnQueue = new();

    public override void _Ready()
    {
        Autoloads.ObjectPool = this;
    }

    public override void _PhysicsProcess(double delta)
    {
        EmptyReturnQueue();
    }

    public T? GetObjectOrNull<T>(PackedScene scene) where T : Node
    {
        Node n = GetObject(scene);
        if(n is not T t)
        {
            ReturnObject(n);
            return null;
        }

        return t;
    }
    public T GetObject<T>(PackedScene scene) where T : Node
    {
        T? @object = GetObjectOrNull<T>(scene);
        if(@object is null)
        {
            GD.PushError($"Given PackedScene {scene.ResourcePath} does not match with type {typeof(T).Name}");
            return null!;
        }
        return @object;
    }

    public Node GetObject(PackedScene scene)
    {
        _objectPoolDict.TryAdd(scene, new());
        Queue<Node> objectQueue = _objectPoolDict[scene];

        //empty. pool new.
        if(objectQueue.Count == 0)
            PoolNewObjects(scene);

        return objectQueue.Dequeue();
    }

    public void ReturnObject(Node n)
    {
        if(!n.IsInstanceValid())
            return;

        string scenePath = n.SceneFilePath;
        if(scenePath == "")
        {
            n.QueueFree();
            GD.PushError("Cannot return object to the object pool if it wasn't created from a PackedScene");
            return;
        }
        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);

        n.GetParent()?.RemoveChild(n);

        _objectPoolReturnQueue.TryAdd(scene, new());
        _objectPoolReturnQueue[scene].Enqueue(n);
    }

    public void PoolNewObjects(PackedScene scene)
    {
        _objectPoolDict.TryAdd(scene, new());
        Queue<Node> objectQueue = _objectPoolDict[scene];
        for(int i = 0; i < OBJECT_POOL_FACTOR; ++i)
            objectQueue.Enqueue(scene.Instantiate());
    }

    public void EmptyReturnQueue()
    {
        List<PackedScene> keys = _objectPoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            _objectPoolDict.TryAdd(key, new());
            Queue<Node> poolQueue = _objectPoolDict[key];

            Queue<Node> returnQueue = _objectPoolReturnQueue[key];
            foreach(Node n in returnQueue)
            {
                if(n.IsInstanceValid())
                    poolQueue.Enqueue(n);
            }
            returnQueue.Clear();
        }
    }

    public override void _Notification(int what)
    {
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
            CleanPool();
    }

    public void CleanPool()
    {
        List<PackedScene> keys;
        
        keys = _objectPoolDict.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = _objectPoolDict[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        _objectPoolDict.Clear();
        
        
        keys = _objectPoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = _objectPoolReturnQueue[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        _objectPoolDict.Clear();
    }
}
