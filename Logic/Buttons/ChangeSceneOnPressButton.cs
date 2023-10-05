using Godot;
using System;

public partial class ChangeSceneOnPressButton : BaseButton
{
    [Export(PropertyHint.File)]
    public string ChangeTo{get; set;} = "";

    public override void _Pressed()
    {
        var err = GetTree().ChangeSceneToFile(ChangeTo);
        if(err != Error.Ok)
            GD.Print($"Error while attempting to change scene: {err}");
    }
}
