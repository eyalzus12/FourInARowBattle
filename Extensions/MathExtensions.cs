using Godot;

namespace FourInARowBattle;

public static class MathExtensions
{
    public static bool IsEqualApprox(this Vector2 a, Vector2 b) => Mathf.IsEqualApprox(a.X, b.X) && Mathf.IsEqualApprox(a.Y, b.Y);
}