using System.Diagnostics.CodeAnalysis;
using Godot;

namespace FourInARowBattle;

public static class GodotObjectExtensions
{
    public static Variant? GetMetaOrNull(this GodotObject o, StringName s) =>
        o.HasMeta(s) ? (Variant?)o.GetMeta(s) : null;
    public static T GetMeta<[MustBeVariant] T>(this GodotObject o, StringName s) =>
        o.GetMeta(s).As<T>();
    public static bool IsInstanceValid([NotNullWhen(true)] this GodotObject? o) =>
        o is not null && GodotObject.IsInstanceValid(o) && !o.IsQueuedForDeletion();
}