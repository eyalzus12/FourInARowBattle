using Godot;

namespace FourInARowBattle;

public partial class SaveGameButton : Button
{
    [Export]
    public Game GameToSave{get; set;} = null!;
    [Export]
    public FileDialog? SaveGamePopup{get; set;}

    public override void _Ready()
    {
        GameToSave.GameBoard.TokenFinishedDrop += () => Disabled = false;
        GameToSave.GameBoard.TokenStartedDrop += () => Disabled = true;

        if(SaveGamePopup is not null)
        {
            SaveGamePopup.FileSelected += (string path) =>
            {
                GameData saveData = GameToSave.SerializeTo();
                Error err = ResourceSaver.Save(saveData, path, ResourceSaver.SaverFlags.Compress);
                if(err != Error.Ok)
                    GD.PushError($"Error {err} while trying to save game");
            };
            
            GetWindow().SizeChanged += () =>
            {
                if(SaveGamePopup.Visible)
                    _Pressed();
            };
        }
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        SaveGamePopup?.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
