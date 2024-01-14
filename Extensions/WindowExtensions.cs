using Godot;

namespace FourInARowBattle;

public static class WindowExtensions
{
    /// <summary>
    /// Get the visible size of the window
    /// </summary>
    /// <param name="w">The window</param>
    /// <returns>The visible size</returns>
    public static Vector2I GetVisibleSize(this Window w) => (Vector2I)w.GetVisibleRect().Size;
    /// <summary>
    /// Get the size of window decorations
    /// </summary>
    /// <param name="w">The window</param>
    /// <returns>The size of window decorations</returns>
    public static Vector2I GetSizeOfDecorations(this Window w) => w.GetSizeWithDecorations() - w.Size;
}