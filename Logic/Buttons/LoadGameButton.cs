using Godot;
using System;

public partial class LoadGameButton : Button
{
    [Export]
    public Game GameToLoadTo{get; set;} = null!;
    [Export]
    public FileDialog LoadGamePopup{get; set;} = null!;

    public override void _Ready()
    {
        LoadGamePopup.FileSelected += (string path) =>
        {
            var saveData = ResourceLoader.Load<GameData>(path);
            GameToLoadTo.DeserializeFrom(saveData);
        };
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        var decorations = GetWindow().GetSizeOfDecorations();
        LoadGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
