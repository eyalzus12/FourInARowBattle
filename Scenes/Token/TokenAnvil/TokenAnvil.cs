using Godot;
using System;

public partial class TokenAnvil : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        //bottom of column
        if(row == board.Rows-1) return;

        TweenFinishedAction = RemoveBottom;
        ConnectTweenFinished();
        void RemoveBottom()
        {
            int row = (board.FindBottomSpot(col) ?? board.Rows)-1;
            board.RemoveToken(row,col);
            board.ApplyColGravity(col);
            board.QueueRedraw();
        }
    }
}
