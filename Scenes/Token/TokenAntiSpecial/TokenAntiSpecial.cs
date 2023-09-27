using Godot;
using System;

public partial class TokenAntiSpecial : TokenBase
{
    public const float DEACTIVATED_DARKEN_AMOUNT = 0.5f;

    private EventBus _eventBus = null!;
    private int? _activeCol;

    public override void OnPlace(Board board, int row, int col)
    {
        _activeCol = col;
        board.TokenPlaced += (Board where, TokenBase who, int _row, int _col) =>
        {
            //for some reason this signal still gets called
            //even after the token is disposed
            //so we do this check to prevent weird stuff happening
            if(!IsInstanceValid(this)) return;

            //active, a token dropped on our same board
            if(_activeCol is not null && board == where && this != who)
            {
                //token is also anti special
                if(who is TokenAntiSpecial)
                {
                    //same team. disable self.
                    if(SameAs(who))
                    {
                        _activeCol = null;
                        Modulate = Modulate.Darkened(DEACTIVATED_DARKEN_AMOUNT);
                    }
                }
                //normal token. same column. disable it.
                else if(_activeCol == _col)
                    who.TweenFinishedAction = null;
            }
        };
    }

    public override void OnLocationUpdate(int row, int col)
    {
        if(_activeCol is not null)
            _activeCol = col;
    }
}
