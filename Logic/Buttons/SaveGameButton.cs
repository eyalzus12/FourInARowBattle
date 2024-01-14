using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// A button that on press, opens a menu to save a game state.
/// </summary>
public partial class SaveGameButton : Button
{
    [Signal]
    public delegate void GameSaveRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private FileDialog _saveGamePopup = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_saveGamePopup);
    }

    private void ConnectSignals()
    {
        _saveGamePopup.FileSelected += OnSaveGamePopupFileSelected;
        GetWindow().SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnSaveGamePopupFileSelected(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GameSaveRequested, path);
    }

    private void OnWindowSizeChanged()
    {
        if(_saveGamePopup.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        _saveGamePopup?.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
