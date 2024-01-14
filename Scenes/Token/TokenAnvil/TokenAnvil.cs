namespace FourInARowBattle;

/// <summary>
/// A special token that removes the bottom token in its column
/// </summary>
public partial class TokenAnvil : TokenBase
{
    /// <summary>
    /// Token finished dropping
    /// </summary>
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
