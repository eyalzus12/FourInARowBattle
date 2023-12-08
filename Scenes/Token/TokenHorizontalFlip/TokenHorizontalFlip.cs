namespace FourInARowBattle;

public partial class TokenHorizontalFlip : TokenOnDropFinish
{
    public override void OnDropFinish(Board board, int row, int col)
    {
        board.FlipRow(row);
        board.ApplyGravity();
    }
}
