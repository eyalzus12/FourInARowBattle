using Godot;
using System;

namespace FourInARowBattle;

public partial class SaveGameButton : Button
{
    [Export]
    public Game GameToSave{get; set;} = null!;
    [Export]
    public FileDialog SaveGamePopup{get; set;} = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(GameToSave);
        ArgumentNullException.ThrowIfNull(SaveGamePopup);
    }

    private void ConnectSignals()
    {
        GameToSave.GameBoard.TokenFinishedDrop += OnGameToSaveGameBoardTokenFinishedDrop;
        GameToSave.GameBoard.TokenStartedDrop += OnGameToSaveGameBoardTokenStartedDrop;
        SaveGamePopup.FileSelected += OnSaveGamePopupFileSelected;
        GetWindow().SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnGameToSaveGameBoardTokenFinishedDrop()
    {
        Disabled = false;
    }

    private void OnGameToSaveGameBoardTokenStartedDrop()
    {
        Disabled = true;
    }

    private void OnSaveGamePopupFileSelected(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GameData saveData = GameToSave.SerializeTo();
        Error err = ResourceSaver.Save(saveData, path, ResourceSaver.SaverFlags.Compress);
        if(err != Error.Ok)
            GD.PushError($"Error {err} while trying to save game");
    }

    private void OnWindowSizeChanged()
    {
        if(SaveGamePopup.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        SaveGamePopup?.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
