using Godot;

namespace FourInARowBattle;

/// <summary>
/// Collision shape extensions
/// </summary>
public static class CollisionShapeExtensions
{
    public static void SetDeferredDisabled(this CollisionShape2D col, bool disabled) => col.SetDeferred(CollisionShape2D.PropertyName.Disabled, disabled);
    public static void SetDeferredDisabled(this CollisionShape3D col, bool disabled) => col.SetDeferred(CollisionShape3D.PropertyName.Disabled, disabled);
}