using Godot;

namespace FourInARowBattle;

public static class Extensions
{
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
