using Godot;

namespace FourInARowBattle;

public static class ControlExtensions
{
    public static void FitLocalRect(this Control c, Rect2 rect)
    {
        Vector2 scale = rect.Size / c.Size;
        c.Scale *= scale;
        c.Position = rect.Position;
    }

    public static void FitGlobalRect(this Control c, Rect2 rect)
    {
        Vector2 scale = rect.Size / c.GetGlobalRect().Size;
        c.Scale *= scale;
        c.GlobalPosition = rect.Position;
    }
}