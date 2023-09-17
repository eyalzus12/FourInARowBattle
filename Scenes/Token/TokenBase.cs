using Godot;
using System;

public partial class TokenBase : Node2D
{
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
}
