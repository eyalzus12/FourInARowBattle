using Godot;

namespace FourInARowBattle;

[GlobalClass]
public partial class TokenDataAntiSpecial : TokenData
{
    [Export]
    public bool TokenEffectIsActive{get; set;}
}
