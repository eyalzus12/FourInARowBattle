using Godot;
using System;

public static class Extensions
{
    public static Variant? GetMetaOrNull(this GodotObject o, StringName s) =>
        o.HasMeta(s)?(Variant?)o.GetMeta(s):null;
    public static T GetMeta<[MustBeVariant] T>(this GodotObject o, StringName s) =>
        o.GetMeta(s).As<T>();
    public static Node GetAutoload(this Node n, NodePath p) =>
        n.GetTree().Root.GetNode(p);
    public static Node? GetAutoloadOrNull(this Node n, NodePath p) =>
        n.GetTree().Root.GetNodeOrNull(p);
    public static T GetAutoload<T>(this Node n, NodePath p) where T : class =>
        n.GetTree().Root.GetNode<T>(p);
    public static T? GetAutoloadOrNull<T>(this Node n, NodePath p) where T : class =>
        n.GetTree().Root.GetNodeOrNull<T>(p);
    public static void StepToEnd(this Tween t) =>
        t.CustomStep(float.PositiveInfinity);
    public static Color GameTurnToColor(this GameTurnEnum g) => g switch
    {
        GameTurnEnum.Player1 => Colors.Red,
        GameTurnEnum.Player2 => Colors.Blue,
        _ => Colors.White
    };
    public static GameTurnEnum GameResultToGameTurn(this GameResultEnum g) => g switch
    {
        GameResultEnum.Player1Win => GameTurnEnum.Player1,
        GameResultEnum.Player2Win => GameTurnEnum.Player2,
        _ => (GameTurnEnum)9999
    };
}
