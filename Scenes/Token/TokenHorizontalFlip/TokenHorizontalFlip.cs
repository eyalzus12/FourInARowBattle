namespace FourInARowBattle;

/// <summary>
/// A special token that flips its row when placed
/// </summary>
public partial class TokenHorizontalFlip : TokenBase
{
    /// <summary>
    /// Token finished dropping
    /// </summary>
    public override void OnDropFinished()
    {
        base.OnDropFinished();
        Board.FlipRow(Row);
        Board.ApplyGravity();
    }
}
