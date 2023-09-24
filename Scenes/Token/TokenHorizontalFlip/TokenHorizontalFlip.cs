using Godot;
using System;

public partial class TokenHorizontalFlip : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        TweenFinishedAction = FlipRow;
        ConnectTweenFinished();
        void FlipRow()
        {
            board.FlipCol(row);
            board.ApplyGravity();
            board.QueueRedraw();
        }
    }
}
