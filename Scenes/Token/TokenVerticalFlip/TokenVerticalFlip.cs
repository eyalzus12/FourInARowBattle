using Godot;
using System;

public partial class TokenVerticalFlip : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        TweenFinishedAction = FlipColumn;
        ConnectTweenFinished();
        void FlipColumn()
        {
            board.FlipCol(col);
            board.ApplyColGravity(col);
            board.QueueRedraw();
        }
    }
}
