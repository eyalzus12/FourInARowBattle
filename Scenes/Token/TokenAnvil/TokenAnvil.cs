using Godot;
using System;

public partial class TokenAnvil : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        //bottom of column
        if(row == board.Rows-1) return;

        Tween? tween = null;
        if(HasMeta(Board.TOKEN_TWEEN_META_NAME))
        {
            tween = (Tween)GetMeta(Board.TOKEN_TWEEN_META_NAME);
            if(!IsInstanceValid(tween))
                tween = null;
        }

        if(tween is not null)
            tween.Finished += () =>
            RemoveBottom(board, col);
        else
            RemoveBottom(board, col);
    }

    private static void RemoveBottom(Board board, int col)
    {
        int row = (board.FindBottomSpot(col) ?? board.Rows)-1;
        //if(row == -1) return;
        board.RemoveToken(row,col);
        board.ApplyColGravity(col);
        board.QueueRedraw();
    }
}
