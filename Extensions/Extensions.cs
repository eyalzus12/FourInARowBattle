using Godot;

namespace FourInARowBattle;

public static class Extensions
{
    public static Color GameTurnToColor(this GameTurnEnum g) => g switch
    {
        GameTurnEnum.PLAYER1 => Colors.Red,
        GameTurnEnum.PLAYER2 => Colors.Blue,
        _ => Colors.White
    };

    public static GameTurnEnum GameResultToGameTurn(this GameResultEnum g) => g switch
    {
        GameResultEnum.PLAYER1_WIN => GameTurnEnum.PLAYER1,
        GameResultEnum.PLAYER2_WIN => GameTurnEnum.PLAYER2,
        _ => (GameTurnEnum)9999
    };
}
