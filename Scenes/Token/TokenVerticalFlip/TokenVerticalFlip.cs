namespace FourInARowBattle;

/// <summary>
/// A token that flips its column when placed
/// </summary>
public partial class TokenVerticalFlip : TokenBase
{
    /// <summary>
    /// Token finished dropping
    /// </summary>
    public override void OnDropFinished()
    {
        base.OnDropFinished();
        Board.FlipCol(Col);
    }
}
