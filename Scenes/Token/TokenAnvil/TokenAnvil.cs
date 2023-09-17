using Godot;
using System;

public partial class TokenAnvil : TokenBase
{
    public override void OnPlace(Board board, int row, int col)
    {
        board.TokenGrid[board.Rows-1,col]?.QueueFree();
        board.TokenGrid[board.Rows-1,col] = null;
        board.ApplyColGravity(col);
    }
}
