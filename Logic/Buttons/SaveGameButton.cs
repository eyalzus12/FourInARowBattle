using Godot;
using System;

public partial class SaveGameButton : Button
{
    [Export]
    public Game GameToSave{get; set;} = null!;
    [Export]
    public FileDialog SaveGamePopup{get; set;} = null!;

    public override void _Ready()
    {
        GameToSave.GameBoard.TweenedTokenCountChanged += (int to) => Disabled = to != 0;

        SaveGamePopup.FileSelected += (string path) =>
        {
            var saveData = GameToSave.SerializeTo();
            var err = ResourceSaver.Save(saveData, path, ResourceSaver.SaverFlags.Compress);
            if(err != Error.Ok)
                GD.Print($"Error {err} while trying to save game");
        };
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        var decorations = GetWindow().GetSizeOfDecorations();
        SaveGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
