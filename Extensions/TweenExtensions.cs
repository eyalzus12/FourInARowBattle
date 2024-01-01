using System.Diagnostics.CodeAnalysis;
using Godot;

namespace FourInARowBattle;

public static class TweenExtensions
{
    public static void StepToEnd(this Tween t) => t.CustomStep(float.PositiveInfinity);
    public static bool IsTweenValid([NotNullWhen(true)] this Tween? t) => t.IsInstanceValid() && t.IsValid();
    
}