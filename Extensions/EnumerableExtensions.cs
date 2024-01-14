using System;
using System.Collections.Generic;
using System.Linq;
using DequeNet;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// IEnumerable extensions
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Convert an IEnumerable to a GodotArray
    /// </summary>
    /// <param name="e">The IEnumerable</param>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <returns>The created GodotArray</returns>
    public static Godot.Collections.Array<T> ToGodotArray<[MustBeVariant] T>(this IEnumerable<T> e) => new(e);
    
    /// <summary>
    /// Convert an IEnumerable to a GodotArray
    /// </summary>
    /// <param name="e">The IEnumerable</param>
    /// <returns>The created GodotArray</returns>
    public static Godot.Collections.Array ToGodotArray(this IEnumerable<Variant> e) => new(e);

    /// <summary>
    /// Convert an IEnumerable to a GodotDictionary
    /// </summary>
    /// <param name="e">The IEnumerable</param>
    /// <param name="selector">A function to map values to keys in the dictionary</param>
    /// <typeparam name="K">The type of keys in the dictionary</typeparam>
    /// <typeparam name="V">The type of values in the dictionary</typeparam>
    /// <returns>The created GodotDictionary</returns>
    public static Godot.Collections.Dictionary<K, V> ToGodotDictionary<[MustBeVariant] K, [MustBeVariant] V>(this IEnumerable<V> e, Func<V, K> selector) where K : notnull => new(e.ToDictionary(selector));

    /// <summary>
    /// Converty an IEnumerable to a Deque
    /// </summary>
    /// <param name="e">The IEnumerable</param>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <returns>The created Deque</returns>
    public static Deque<T> ToDeque<T>(this IEnumerable<T> e) => new(e);

    /// <summary>
    /// Perform an operation for each element in an IEnumerable
    /// </summary>
    /// <param name="e">The IEnumerable</param>
    /// <param name="a">The operation to perform</param>
    /// <typeparam name="T">The type of elements</typeparam>
    public static void ForEach<T>(this IEnumerable<T> e, Action<T> a){foreach(T ee in e)a(ee);}

    /// <summary>
    /// Push right multiple values into a Deque
    /// </summary>
    /// <param name="deque">The Deque</param>
    /// <param name="enumerable">The elements to push</param>
    /// <typeparam name="T">The type of elements</typeparam>
    public static void PushRightRange<T>(this Deque<T> deque, IEnumerable<T> enumerable) => enumerable.ForEach(deque.PushRight);
    /// <summary>
    /// Push left multiple values into a Deque
    /// </summary>
    /// <param name="deque">The Deque</param>
    /// <param name="enumerable">The elements to push</param>
    /// <typeparam name="T">The type of elements</typeparam>
    public static void PushLeftRange<T>(this Deque<T> deque, IEnumerable<T> enumerable) => enumerable.ForEach(deque.PushLeft);

}