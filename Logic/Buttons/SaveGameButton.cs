using Godot;

namespace FourInARowBattle;

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
            GameData saveData = GameToSave.SerializeTo();
            Error err = ResourceSaver.Save(saveData, path, ResourceSaver.SaverFlags.Compress);
            if(err != Error.Ok)
                GD.PushError($"Error {err} while trying to save game");
        };
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        SaveGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
