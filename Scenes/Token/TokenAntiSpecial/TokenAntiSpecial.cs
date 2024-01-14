using System;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A special token that prevents special effects in its column.
/// If more than two are placed by the same player, the oldest one stops working.
/// </summary>
public partial class TokenAntiSpecial : TokenBase
{
    //how much to darken when not active
    public const float DEACTIVATED_DARKEN_AMOUNT = 0.5f;
    //whether effect is active
    private bool _active = false;

    private bool _signalConnected = false;

    /// <summary>
    /// Ran when token spawns into the board
    /// </summary>
    /// <param name="board">The board</param>
    /// <param name="row">The row</param>
    /// <param name="col">The column</param>
    public override void TokenSpawn(Board board, int row, int col)
    {
        base.TokenSpawn(board, row, col);

        if(_signalConnected) return;
        
        //when token is placed...
        Board.TokenPlaced += (TokenBase who, int _row, int _col) =>
        {
            //it's possible for this signal to be invoked while this token is inside the pool
            //so we do this check to prevent removed tokens from doing anything
            if(!this.IsInstanceValid() || !IsInsideTree()) return;
            //if token is me or im inactivce, return
            if(this == who || !_active) return;

            //if token is also anti special
            if(who is TokenAntiSpecial)
            {
                //if the token is the same color
                if(SameAs(who))
                {
                    //deactivate
                    _active = false;
                    Modulate = Modulate.Darkened(DEACTIVATED_DARKEN_AMOUNT);
                }
            }
            //otherwise, it is on the same column
            else if(Col == _col)
            {
                //deactivate it
                who.ActivatedPower = true;
            }
        };
        
        _signalConnected = true;
        _active = true;
    }

    /// <summary>
    /// Load token data
    /// </summary>
    /// <param name="board">The board of the token</param>
    /// <param name="data">The token data</param>
    public override void DeserializeFrom(Board board, TokenData data)
    {
        base.DeserializeFrom(board, data);
        if(data is not TokenDataAntiSpecial)
        {
            GD.PushError(
                "Anti special tokens require token data "+
                $"of type {nameof(TokenDataAntiSpecial)}, "+
                $"but there was an attempt to create one with type {data.GetType().Name}"
            );
            return;
        }
        TokenDataAntiSpecial adata = (TokenDataAntiSpecial)data;
        _active = adata.TokenEffectIsActive;
    }

    /// <summary>
    /// Save current token state
    /// </summary>
    /// <returns>The token state</returns>
    public override TokenData SerializeTo() => new TokenDataAntiSpecial()
    {
        TokenScenePath = SceneFilePath,
        TokenColor = TokenColor,
        TokenModulate = Modulate,
        GlobalPosition = GlobalPosition,
        TokenEffectIsActive = _active
    };
}
