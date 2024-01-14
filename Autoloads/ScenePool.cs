using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FourInARowBattle;

/// <summary>
/// An object pool singleton.
/// </summary>
public partial class ScenePool : Node
{
    /// <summary>
    /// How many new scenes to create if pool is empty
    /// </summary>
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

    /// <summary>
    /// Get a new instance of the scene. Returns null if type is not T
    /// </summary>
    /// <param name="scene">The scene</param>
    /// <typeparam name="T">The desired return type</typeparam>
    /// <returns>An instance of the scene, or null if its type is not T</returns>
    public T? GetSceneOrNull<T>(PackedScene scene) where T : Node
    {
        ArgumentNullException.ThrowIfNull(scene);
        Node n = GetScene(scene);
        if(n is not T t)
        {
            ReturnScene(n);
            return null;
        }

        return t;
    }

    /// <summary>
    /// Get a new instance of the scene. Errors if type is not T.
    /// </summary>
    /// <param name="scene">The scene</param>
    /// <typeparam name="T">The desired return type</typeparam>
    /// <returns>An instance of the scene</returns>
    public T GetScene<T>(PackedScene scene) where T : Node
    {
        ArgumentNullException.ThrowIfNull(scene);
        T? sceneNode = GetSceneOrNull<T>(scene);
        if(sceneNode is null)
        {
            GD.PushError($"Given PackedScene {scene.ResourcePath} does not match with type {typeof(T).Name}");
            return null!;
        }
        return sceneNode;
    }

    /// <summary>
    /// Get a new instance of the scene.
    /// </summary>
    /// <param name="scene">The scene</param>
    /// <returns>An instance of the scene</returns>
    public Node GetScene(PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _scenePoolDict.TryAdd(scene, new());
        Queue<Node> sceneQueue = _scenePoolDict[scene];

        //empty. pool new.
        if(sceneQueue.Count == 0)
            PoolNewScenes(scene);

        return sceneQueue.Dequeue();
    }

    /// <summary>
    /// Dispose of scene instance, putting it back in the pool
    /// </summary>
    /// <param name="n">The scene instance</param>
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

        n.GetParent()?.RemoveChildDeferred(n);

        _scenePoolReturnQueue.TryAdd(scene, new());
        _scenePoolReturnQueue[scene].Enqueue(n);
    }

    /// <summary>
    /// Helper functin. Create new instances of the scene in the pool.
    /// </summary>
    /// <param name="scene"></param>
    private void PoolNewScenes(PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _scenePoolDict.TryAdd(scene, new());
        Queue<Node> sceneQueue = _scenePoolDict[scene];
        for(int i = 0; i < SCENE_POOL_FACTOR; ++i)
            sceneQueue.Enqueue(scene.Instantiate());
    }

    /// <summary>
    /// Helper function. Move all disposed instances from the return queue into the pool.
    /// </summary>
    private void EmptyReturnQueue()
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
        //when the scene pool is disposed, make sure to also dispose pooled nodes
        if(what == NotificationExitTree || what == NotificationCrash || what == NotificationWMCloseRequest)
            CleanPool();
    }

    /// <summary>
    /// Dispose of all pooled nodes.
    /// </summary>
    private void CleanPool()
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
