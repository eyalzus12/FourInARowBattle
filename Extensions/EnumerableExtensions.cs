using System;
using System.Collections.Generic;
using System.Linq;
using DequeNet;
using Godot;

namespace FourInARowBattle;

public static class EnumerableExtensions
{   
    public static Godot.Collections.Array<T> ToGodotArray<[MustBeVariant] T>(this IEnumerable<T> e) => new(e);
    public static Godot.Collections.Array ToGodotArray(this IEnumerable<Variant> e) => new(e);
    public static Godot.Collections.Dictionary<K, V> ToGodotDictionary<[MustBeVariant] K, [MustBeVariant] V>(this IEnumerable<V> e, Func<V, K> selector) where K : notnull => new(e.ToDictionary(selector));
    public static Godot.Collections.Dictionary<Variant, V> ToGodotDictionary<[MustBeVariant] V>(this IEnumerable<V> e, Func<V, Variant> selector) => new(e.ToDictionary(selector));
    public static Godot.Collections.Dictionary<K, Variant> ToGodotDictionary<[MustBeVariant] K>(this IEnumerable<Variant> e, Func<Variant, K> selector) where K : notnull => new(e.ToDictionary(selector));
    public static Godot.Collections.Dictionary<Variant, Variant> ToGodotDictionary(this IEnumerable<Variant> e, Func<Variant, Variant> selector) => new(e.ToDictionary(selector));
    public static Deque<T> ToDeque<T>(this IEnumerable<T> e) => new(e);

    public static bool ContainsNotNull<T>(this IReadOnlySet<T> set, T? t) => t is not null && set.Contains(t);
    public static bool ContainsKeyNotNull<K, V>(this IReadOnlyDictionary<K, V> dict, K? k) => k is not null && dict.ContainsKey(k);
}