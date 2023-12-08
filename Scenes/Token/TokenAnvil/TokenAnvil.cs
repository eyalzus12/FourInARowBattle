namespace FourInARowBattle;

public partial class TokenAnvil : TokenOnDropFinish
{
    public override void OnDropFinish(Board board, int row, int col)
    {
        //bottom of column
        if(row == board.Rows-1) return;
        //get location to remove
        int removeRow = (board.FindBottomSpot(col) ?? board.Rows)-1;
        //there are no tokens in the column
        //this can happen if we get a four-in-a-row while dropping this token
        if(removeRow == -1) return;
        //remove
        board.RemoveToken(removeRow,col);
        board.ApplyColGravity(col);
    }
}
