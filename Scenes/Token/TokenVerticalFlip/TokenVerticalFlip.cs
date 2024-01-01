namespace FourInARowBattle;

public partial class TokenVerticalFlip : TokenBase
{
    public override void OnDropFinished()
    {
        base.OnDropFinished();
        Board.FlipCol(Col);
    }
}
