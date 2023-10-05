using Godot;
using System;

public partial class ChangeSceneOnPressButton : BaseButton
{
    [Export]
    public PackedScene ChangeTo{get; set;} = null!;

    public override void _Pressed()
    {
        var err = GetTree().ChangeSceneToPacked(ChangeTo);
        if(err != Error.Ok)
            GD.Print($"Error while attempting to change scene: {err}");
    }
}
