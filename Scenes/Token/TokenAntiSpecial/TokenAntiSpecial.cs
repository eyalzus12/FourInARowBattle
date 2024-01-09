using System;
using Godot;

namespace FourInARowBattle;

public partial class TokenAntiSpecial : TokenBase
{
    public const float DEACTIVATED_DARKEN_AMOUNT = 0.5f;
    private bool _active = false;

    public override void TokenSpawn(Board board, int row, int col)
    {
        base.TokenSpawn(board, row, col);

        Board.TokenPlaced += (TokenBase who, int _row, int _col) =>
        {
            if(!this.IsInstanceValid() || this == who || !_active) return;
            //token is also anti special
            if(who is TokenAntiSpecial)
            {
                if(SameAs(who))
                {
                    _active = false;
                    Modulate = Modulate.Darkened(DEACTIVATED_DARKEN_AMOUNT);
                }
            }
            else if(Col == _col)
            {
                who.ActivatedPower = true;
            }
        };
        _active = true;
    }

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

    public override TokenData SerializeTo() => new TokenDataAntiSpecial()
    {
        TokenScenePath = SceneFilePath,
        TokenColor = TokenColor,
        TokenModulate = Modulate,
        GlobalPosition = GlobalPosition,
        TokenEffectIsActive = _active
    };
}
