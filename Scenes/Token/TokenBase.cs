using Godot;
using System;

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
        Modulate = TokenColor;
    }

    public virtual void OnPlace(Board board, int row, int col)
    {
        
    }

    public Action? TweenFinishedAction{get; protected set;}
    public void ConnectTweenFinished()
    {
        if(TweenFinishedAction is null) return;
        if(!IsInstanceValid(TokenTween)) TokenTween = null;
        if(TokenTween is not null)
            TokenTween.Finished += () =>
            {
                if(TweenFinishedAction is not null)
                    TweenFinishedAction();
                TweenFinishedAction = null;
            };
        else
            TweenFinishedAction();
    }
}
