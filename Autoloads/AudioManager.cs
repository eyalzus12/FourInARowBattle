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
        CreateNewPool(ref _playersPool, PLAYER_POOL_FACTOR);
        CreateNewPool(ref _players2DPool, PLAYER2D_POOL_FACTOR);
        CreateNewPool(ref _players3DPool, PLAYER3D_POOL_FACTOR);
    }

    private static readonly StringName FinishedSignal = "finished";

    private void CreateNewPool<T>(ref Pool<T> pool, int factor) where T : Node, new()
    {
        pool = new(factor, OnCreateInside(pool), OnDeleteInside<T>());
    }

    private Action<T> OnCreateInside<T>(Pool<T> inside) where T : Node, new() =>
        (T t) =>
        {
            if(!t.IsInsideTree())
            {
                AddChild(t);
                t.Connect(FinishedSignal, Callable.From(() => inside.ReturnObject(t)));
            }
        };
    
    private static Action<T> OnDeleteInside<T>() where T : Node, new() =>
        (T t) =>
        {
            t.QueueFree();
        };

    public class Pool<T> where T : new()
    {
        public int PoolFactor{get; set;}
        public Action<T>? OnCreate{get; set;}
        public Action<T>? OnDelete{get; set;}

        public Pool(int factor, Action<T>? onCreate = null, Action<T>? onDelete = null)
        {
            PoolFactor = factor;
            OnCreate = onCreate;
            OnDelete = onDelete;
        }

        public Queue<T> PoolQueue{get; set;} = new();
        public HashSet<T> PoolQueueSet{get; set;} = new();
        public HashSet<T> PoolList{get; set;} = new();

        public T GetObject()
        {
            if(PoolQueue.Count == 0)
            {
                for(int i = 0; i < PoolFactor; ++i)
                {
                    T newT = new();
                    if(OnCreate is not null)
                        OnCreate(newT);
                    ReturnObject(newT);
                }
            }

            T resT = PoolQueue.Dequeue();
            PoolQueueSet.Remove(resT);
            return resT;
        }

        public void ReturnObject(T t)
        {
            if(!PoolQueueSet.Contains(t))
            {
                PoolQueue.Enqueue(t);
                PoolQueueSet.Add(t);
            }

            if(!PoolList.Contains(t))
            {
                PoolList.Add(t);
            }
        }

        public void Cleanup()
        {
            if(OnDelete is not null) foreach(T t in PoolList) OnDelete(t);
            PoolList.Clear();
            PoolQueue.Clear();
            PoolQueueSet.Clear();
        }
    }
}
