using Godot;
using System;
using System.Diagnostics.CodeAnalysis;

public static class Extensions
{
    //useful meta functions. type same GetMetaOrNull should be possible,
    //but i've messed around a lot with attributes and couldn't get the compiler to agree
    public static Variant? GetMetaOrNull(this GodotObject o, StringName s) =>
        o.HasMeta(s) ? (Variant?)o.GetMeta(s) : null;
    public static T GetMeta<[MustBeVariant] T>(this GodotObject o, StringName s) =>
        o.GetMeta(s).As<T>();
    //shorthand for getting autoloads. i don't use these anywhere.
    public static Node GetAutoload(this Node n, NodePath p) =>
        n.GetTree().Root.GetNode(p);
    public static Node? GetAutoloadOrNull(this Node n, NodePath p) =>
        n.GetTree().Root.GetNodeOrNull(p);
    public static T GetAutoload<T>(this Node n, NodePath p) where T : class =>
        n.GetTree().Root.GetNode<T>(p);
    public static T? GetAutoloadOrNull<T>(this Node n, NodePath p) where T : class =>
        n.GetTree().Root.GetNodeOrNull<T>(p);
    //useful: make a tween finish immedietly.
    public static void StepToEnd(this Tween t) =>
        t.CustomStep(float.PositiveInfinity);
    //common conversions between enums and stuff
    //the 9999 is a value assumed to be invalid
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

    //the default IsInstanceValid does not tell the compiler
    //that the paramater is not null if it returns true
    //so this is a wrapper that does that
    //why do extension methods work with null? that's wack
    public static bool IsInstanceValid([NotNullWhen(true)] this GodotObject? o) =>
        o is not null && GodotObject.IsInstanceValid(o);
    public static bool IsTweenValid([NotNullWhen(true)] this Tween? t) =>
        t.IsInstanceValid() && t.IsValid();
}
