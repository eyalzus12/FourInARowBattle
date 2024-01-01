namespace FourInARowBattle;

public partial class TokenAnvil : TokenBase
{
    public override void OnDropFinished()
    {
        base.OnDropFinished();
        //bottom of column
        if(Row == Board.Rows-1) return;
        //get location to remove
        int removeRow = (Board.FindBottomSpot(Col) ?? Board.Rows)-1;
        //there are no tokens in the column
        //this can happen if we get a four-in-a-row while dropping this token
        if(removeRow == -1) return;
        //remove
        Board.RemoveToken(removeRow,Col);
        Board.ApplyColGravity(Col);
    }
}
