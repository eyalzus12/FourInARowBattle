using Godot;

namespace FourInARowBattle;

/// <summary>
/// Misc extensions
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Convert GameTurnEnum value to a color
    /// </summary>
    /// <param name="g">The enum value</param>
    /// <returns>The color</returns>
    public static Color GameTurnToColor(this GameTurnEnum g) => g switch
    {
        GameTurnEnum.PLAYER1 => Colors.Red,
        GameTurnEnum.PLAYER2 => Colors.Blue,
        _ => Colors.White
    };

    /// <summary>
    /// Convert GameResultEnum value to a GameTurnEnum value
    /// </summary>
    /// <param name="g">the GameResultEnum value</param>
    /// <returns>The resulting GameTurnEnum value</returns>
    public static GameTurnEnum GameResultToGameTurn(this GameResultEnum g) => g switch
    {
        GameResultEnum.PLAYER1_WIN => GameTurnEnum.PLAYER1,
        GameResultEnum.PLAYER2_WIN => GameTurnEnum.PLAYER2,
        //an assumed-invalid value
        _ => (GameTurnEnum)9999
    };
}
