using Godot;
using System;

public partial class TokenDataAntiSpecial : TokenData
{
    [Export]
    public int TokenColumn{get; set;}
    [Export]
    public bool TokenEffectIsActive{get; set;}
}
