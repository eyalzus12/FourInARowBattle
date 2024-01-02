namespace FourInARowBattle;

public partial class TokenHorizontalFlip : TokenBase
{
    public override void OnDropFinished()
    {
        base.OnDropFinished();
        Board.FlipRow(Row);
        Board.ApplyGravity();
    }
}
