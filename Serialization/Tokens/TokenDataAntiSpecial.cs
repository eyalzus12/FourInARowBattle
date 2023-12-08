using Godot;

namespace FourInARowBattle;

public partial class TokenDataAntiSpecial : TokenData
{
    [Export]
    public int TokenColumn{get; set;}
    [Export]
    public bool TokenEffectIsActive{get; set;}
}
