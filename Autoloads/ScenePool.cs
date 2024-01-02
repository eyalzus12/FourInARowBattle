using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class ScenePool : Node
{
    //how many new scenes to create if pool is empty
    public const int SCENE_POOL_FACTOR = 3;

    private readonly Dictionary<PackedScene, Queue<Node>> _scenePoolDict = new();
    private readonly Dictionary<PackedScene, Queue<Node>> _scenePoolReturnQueue = new();

    public override void _Ready()
    {
        Autoloads.ScenePool = this;
    }

    public override void _PhysicsProcess(double delta)
    {
        EmptyReturnQueue();
    }

    public T? GetSceneOrNull<T>(PackedScene scene) where T : Node
    {
        Node n = GetScene(scene);
        if(n is not T t)
        {
            ReturnScene(n);
            return null;
        }

        return t;
    }
    public T GetScene<T>(PackedScene scene) where T : Node
    {
        T? sceneNode = GetSceneOrNull<T>(scene);
        if(sceneNode is null)
        {
            GD.PushError($"Given PackedScene {scene.ResourcePath} does not match with type {typeof(T).Name}");
            return null!;
        }
        return sceneNode;
    }

    public Node GetScene(PackedScene scene)
    {
        _scenePoolDict.TryAdd(scene, new());
        Queue<Node> sceneQueue = _scenePoolDict[scene];

        //empty. pool new.
        if(sceneQueue.Count == 0)
            PoolNewScenes(scene);

        return sceneQueue.Dequeue();
    }

    public void ReturnScene(Node n)
    {
        if(!n.IsInstanceValid())
            return;

        string scenePath = n.SceneFilePath;
        if(scenePath == "")
        {
            n.QueueFree();
            GD.PushError("Cannot return scene to the scene pool if it wasn't created from a PackedScene");
            return;
        }
        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);

        n.GetParent()?.CallDeferred(Node.MethodName.RemoveChild, n);

        _scenePoolReturnQueue.TryAdd(scene, new());
        _scenePoolReturnQueue[scene].Enqueue(n);
    }

    public void PoolNewScenes(PackedScene scene)
    {
        _scenePoolDict.TryAdd(scene, new());
        Queue<Node> sceneQueue = _scenePoolDict[scene];
        for(int i = 0; i < SCENE_POOL_FACTOR; ++i)
            sceneQueue.Enqueue(scene.Instantiate());
    }

    public void EmptyReturnQueue()
    {
        List<PackedScene> keys = _scenePoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            _scenePoolDict.TryAdd(key, new());
            Queue<Node> poolQueue = _scenePoolDict[key];

            Queue<Node> returnQueue = _scenePoolReturnQueue[key];
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
        
        keys = _scenePoolDict.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = _scenePoolDict[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        _scenePoolDict.Clear();
        
        
        keys = _scenePoolReturnQueue.Keys.ToList();
        foreach(PackedScene key in keys)
        {
            Queue<Node> q = _scenePoolReturnQueue[key];
            foreach(Node n in q)
            {
                n.QueueFree();
            }
            q.Clear();
        }
        _scenePoolDict.Clear();
    }
}
