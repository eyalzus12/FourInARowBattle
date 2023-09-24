using Godot;
using System;

public partial class TokenVerticalFlip : TokenOnDropFinish
{
    public override void OnDropFinish(Board board, int row, int col)
    {
        board.FlipCol(col);
        board.ApplyColGravity(col);
    }
}
