using Godot;

namespace FourInARowBattle;

[GlobalClass]
public partial class TokenCounterData : Resource
{
    [Export]
    public int TokenCount{get; set;}
}
