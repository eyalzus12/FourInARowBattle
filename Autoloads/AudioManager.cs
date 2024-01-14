using Godot;
using System;
using System.Collections.Generic;

namespace FourInARowBattle;

/// <summary>
/// Singleton class exposing audio player pools
/// </summary>
public partial class AudioManager : Node
{
    /// <summary>
    /// When audio player pool is empty, create this many new audio players
    /// </summary>
    public const int PLAYER_POOL_FACTOR = 3;
    /// <summary>
    /// When 2D audio player pool is empty, create this many new 2D audio players
    /// </summary>
    public const int PLAYER2D_POOL_FACTOR = 3;
    /// <summary>
    /// When 3D audio player pool is empty, create this many new 3D audio players
    /// </summary>
    public const int PLAYER3D_POOL_FACTOR = 3;

    private Pool<AudioStreamPlayer> _playersPool = null!;
    private Pool<AudioStreamPlayer2D> _players2DPool = null!;
    private Pool<AudioStreamPlayer3D> _players3DPool = null!;

    /// <summary>
    /// Pool for audio players
    /// </summary>
    public Pool<AudioStreamPlayer> AudioPlayersPool => _playersPool;
    /// <summary>
    /// Pool for 2D audio players
    /// </summary>
    public Pool<AudioStreamPlayer2D> AudioPlayers2DPool => _players2DPool;
    /// <summary>
    /// Pool for 3D audio players
    /// </summary>
    public Pool<AudioStreamPlayer3D> AudioPlayers3DPool => _players3DPool;

    /// <summary>
    /// Called after entering tree. Setup pools.
    /// </summary>
    public override void _Ready()
    {
        Autoloads.AudioManager = this;
        _playersPool = new(PLAYER_POOL_FACTOR, OnCreate(() => _playersPool));
        _players2DPool = new(PLAYER2D_POOL_FACTOR, OnCreate(() => _players2DPool));
        _players3DPool = new(PLAYER3D_POOL_FACTOR, OnCreate(() => _players3DPool));
    }

    private static readonly StringName FINISHED_SIGNAL = AudioStreamPlayer.SignalName.Finished;

    //We want to return any player that finished into the pool.
    //However we can't pass in the pool before we initialize it.
    //Into this function we pass a function that returns the pool.
    private Action<T> OnCreate<T>(Func<Pool<T>> pool) where T : Node, new() => 
        (T t) =>
        {
            if(!t.IsInsideTree())
            {
                AddChild(t);
                t.Connect(FINISHED_SIGNAL, Callable.From(() => pool().ReturnObject(t)));
            }
        };

    /// <summary>
    /// Internal pool class
    /// </summary>
    /// <typeparam name="T">The type of objects in the pool</typeparam>
    public class Pool<T> where T : new()
    {
        private readonly int _poolFactor;
        private readonly Action<T>? _onCreate;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="factor">How many new objects to load when pool is empty</param>
        /// <param name="onCreate">Function to run on each created object</param>
        public Pool(int factor, Action<T>? onCreate = null)
        {
            _poolFactor = factor;
            _onCreate = onCreate;
        }

        private readonly Queue<T> _poolQueue = new();
        public readonly HashSet<T> _poolQueueSet = new();
        public readonly HashSet<T> _poolList = new();

        /// <summary>
        /// Get a new pooled object
        /// </summary>
        /// <returns>A new object</returns>
        public T GetObject()
        {
            if(_poolQueue.Count == 0)
            {
                for(int i = 0; i < _poolFactor; ++i)
                {
                    T newT = new();
                    if(_onCreate is not null)
                        _onCreate(newT);
                    _poolQueue.Enqueue(newT);
                    _poolQueueSet.Add(newT);
                    _poolList.Add(newT);
                }
            }

            T resT = _poolQueue.Dequeue();
            _poolQueueSet.Remove(resT);
            return resT;
        }

        /// <summary>
        /// Dispose of an object, putting it back into the pool
        /// </summary>
        /// <param name="t">The object</param>
        public void ReturnObject(T t)
        {
            if(t is null)
                return;
            if(!_poolList.Contains(t))
                return;
            if(_poolQueueSet.Contains(t))
                return;
            
            _poolQueue.Enqueue(t);
            _poolQueueSet.Add(t);
        }
    }
}
