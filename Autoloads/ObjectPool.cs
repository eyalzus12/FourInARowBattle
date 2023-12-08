using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class ObjectPool : Node
{
    //how many new objects to create if pool is empty
    public const int OBJECT_POOL_FACTOR = 3;

    public Dictionary<PackedScene, Queue<Node>> ObjectPoolDictionary{get; protected set;} = new();
    public Dictionary<PackedScene, Queue<Node>> ObjectPoolReturnQueue{get; protected set;} = new();

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
    public T GetObject<T>(PackedScene scene) where T : Node => GetObjectOrNull<T>(scene) ?? throw new InvalidCastException($"Given PackedScene {scene.ResourcePath} does not match with type {typeof(T).Name}");

    public Node GetObject(PackedScene scene)
    {
        ObjectPoolDictionary.TryAdd(scene, new());
        Queue<Node> objectQueue = ObjectPoolDictionary[scene];

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

        ObjectPoolReturnQueue.TryAdd(scene, new());
        ObjectPoolReturnQueue[scene].Enqueue(n);
    }

    public void PoolNewObjects(PackedScene scene)
    {
        ObjectPoolDictionary.TryAdd(scene, new());
        Queue<Node> objectQueue = ObjectPoolDictionary[scene];
        for(int i = 0; i < OBJECT_POOL_FACTOR; ++i)
            objectQueue.Enqueue(scene.Instantiate());
    }

    public void EmptyReturnQueue()
    {
        List<PackedScene> keys = ObjectPoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            ObjectPoolDictionary.TryAdd(key, new());
            Queue<Node> poolQueue = ObjectPoolDictionary[key];

            Queue<Node> returnQueue = ObjectPoolReturnQueue[key];
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
        
        keys = ObjectPoolDictionary.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = ObjectPoolDictionary[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        ObjectPoolDictionary.Clear();
        
        
        keys = ObjectPoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = ObjectPoolReturnQueue[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        ObjectPoolDictionary.Clear();
    }
}
