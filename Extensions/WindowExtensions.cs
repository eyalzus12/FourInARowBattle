using Godot;

namespace FourInARowBattle;

public static class WindowExtensions
{
    public static Vector2I GetVisibleSize(this Window w) => (Vector2I)w.GetVisibleRect().Size;
    public static Vector2I GetSizeOfDecorations(this Window w) => w.GetSizeWithDecorations() - w.Size;
}