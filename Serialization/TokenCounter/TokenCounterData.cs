using Godot;

namespace FourInARowBattle;

/// <summary>
/// Token counter data resource
/// </summary>
[GlobalClass]
public partial class TokenCounterData : Resource
{
    /// <summary>
    /// How many tokens there are
    /// </summary>
    [Export]
    public int TokenCount{get; set;}
}
