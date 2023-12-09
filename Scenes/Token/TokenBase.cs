using Godot;
using System;

namespace FourInARowBattle;

public partial class TokenBase : Node2D
{
    [Export(PropertyHint.MultilineText)]
    public string TokenDescription{get; set;} = "NO DESCRIPTION SET FOR THIS TOKEN";

    public Tween? TokenTween{get; set;}

    private Color _tokenColor = Colors.White;
    public Color TokenColor
    {
        get => _tokenColor;
        set
        {
            _tokenColor = value;
            Modulate = value;
        }
    }

    public virtual GameResultEnum Result
    {
        get
        {
            if(TokenColor == Colors.Red) return GameResultEnum.Player1Win;
            if(TokenColor == Colors.Blue) return GameResultEnum.Player2Win;
            return GameResultEnum.None;
        }
    }

    public virtual bool SameAs(TokenBase? t) => t is not null && TokenColor == t.TokenColor;

    public override void _Ready()
    {
        //avoid overriding previous modulate
        if(Modulate == Colors.White)
            Modulate = TokenColor;
    }

    public virtual void OnPlace(Board board, int row, int col)
    {
        
    }

    public virtual void OnLocationUpdate(Board board, int row, int col)
    {

    }

    public Action? TweenFinishedAction{get; set;}
    public void ConnectTweenFinished()
    {
        if(TweenFinishedAction is null) return;
        if(!TokenTween.IsInstanceValid()) TokenTween = null;
        if(TokenTween is not null)
        {
            //bind to current tween
            Tween bind = TokenTween;

            TokenTween.Finished += () =>
            {
                if(TweenFinishedAction is not null)
                    TweenFinishedAction();
                TweenFinishedAction = null;

                //make sure old tween doesn't destroy new one
                if(TokenTween == bind)
                {
                    TokenTween?.Kill();
                    TokenTween?.Dispose();
                    TokenTween = null;
                }
            };
        }
        else
            TweenFinishedAction();
    }

    public virtual void DeserializeFrom(Board board, TokenData data)
    {
        TokenColor = data.TokenColor;
        Modulate = data.TokenModulate;
        GlobalPosition = data.GlobalPosition;
    }

    public virtual TokenData SerializeTo() => new()
    {
        TokenScenePath = SceneFilePath,
        TokenColor = TokenColor,
        TokenModulate = Modulate,
        GlobalPosition = GlobalPosition
    };
}
