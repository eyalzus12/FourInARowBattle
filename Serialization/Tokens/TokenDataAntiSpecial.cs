using Godot;

namespace FourInARowBattle;

/// <summary>
/// Special data resource class for TokenAntiSpecial
/// </summary>
[GlobalClass]
public partial class TokenDataAntiSpecial : TokenData
{
    /// <summary>
    /// Whether the anti-special effect is active
    /// </summary>
    [Export]
    public bool TokenEffectIsActive{get; set;}
}
