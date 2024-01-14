using System.Diagnostics.CodeAnalysis;
using Godot;

namespace FourInARowBattle;

public static class TweenExtensions
{
    /// <summary>
    /// End the tween
    /// </summary>
    /// <param name="t">The tween to end</param>
    public static void StepToEnd(this Tween t) => t.CustomStep(float.PositiveInfinity);
    /// <summary>
    /// Check if a tween is a valid instance and a valid tween
    /// </summary>
    /// <param name="t">The tween</param>
    /// <returns>Whether the tween is valid</returns>
    public static bool IsTweenValid([NotNullWhen(true)] this Tween? t) => t.IsInstanceValid() && t.IsValid();
    /// <summary>
    /// Kill a tween if it is valid
    /// </summary>
    /// <param name="t">The tween to kill</param>
    /// <returns>Whether it was valid</returns>
    public static bool KillIfValid([NotNullWhen(true)] this Tween? t)
    {
        if(t.IsTweenValid())
        {
            t.Kill();
            return true;
        }
        return false;
    }
}