using Godot;
using System;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class AudioManager : Node
{
    public const int PLAYER_POOL_FACTOR = 3;
    public const int PLAYER2D_POOL_FACTOR = 3;
    public const int PLAYER3D_POOL_FACTOR = 3;

    private Pool<AudioStreamPlayer> _playersPool = null!;
    private Pool<AudioStreamPlayer2D> _players2DPool = null!;
    private Pool<AudioStreamPlayer3D> _players3DPool = null!;

    public Pool<AudioStreamPlayer> PlayersPool => _playersPool;
    public Pool<AudioStreamPlayer2D> Players2DPool => _players2DPool;
    public Pool<AudioStreamPlayer3D> Players3DPool => _players3DPool;

    public override void _Ready()
    {
        Autoloads.AudioManager = this;
        _playersPool = new(PLAYER_POOL_FACTOR, OnCreate(() => _playersPool));
        _players2DPool = new(PLAYER2D_POOL_FACTOR, OnCreate(() => _players2DPool));
        _players3DPool = new(PLAYER3D_POOL_FACTOR, OnCreate(() => _players3DPool));
    }

    private static readonly StringName FinishedSignal = "finished";

    //hack: give a function that returns the pool to fetch the pool value later
    //instead of right now, when it is null
    private Action<T> OnCreate<T>(Func<Pool<T>> pool) where T : Node, new() => 
        (T t) =>
        {
            if(!t.IsInsideTree())
            {
                AddChild(t);
                t.Connect(FinishedSignal, Callable.From(() => pool().ReturnObject(t)));
            }
        };

    public class Pool<T> where T : new()
    {
        private readonly int _poolFactor;
        private readonly Action<T>? _onCreate;

        public Pool(int factor, Action<T>? onCreate = null)
        {
            _poolFactor = factor;
            _onCreate = onCreate;
        }

        private readonly Queue<T> _poolQueue = new();
        public readonly HashSet<T> _poolQueueSet = new();
        public readonly HashSet<T> _poolList = new();

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

        public void ReturnObject(T t)
        {
            if(!_poolList.Contains(t))
                return;
            if(_poolQueueSet.Contains(t))
                return;
            
            _poolQueue.Enqueue(t);
            _poolQueueSet.Add(t);
        }
    }
}
