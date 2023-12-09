namespace FourInARowBattle;

public abstract partial class TokenOnDropFinish : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        TweenFinishedAction = () => OnDropFinish(board, row, col);
        ConnectTweenFinished();
    }

    public override void OnLocationUpdate(Board board, int row, int col)
    {
        if(TweenFinishedAction is not null)
            TweenFinishedAction = () => OnDropFinish(board, row, col);
    }

    public abstract void OnDropFinish(Board board, int row, int col);
}
