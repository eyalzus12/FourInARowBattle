using Godot;
using System;

namespace FourInARowBattle;

public partial class SaveGameButton : Button
{
    [Signal]
    public delegate void GameSaveRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private FileDialog SaveGamePopup = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(SaveGamePopup);
    }

    private void ConnectSignals()
    {
        SaveGamePopup.FileSelected += OnSaveGamePopupFileSelected;
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
        if(SaveGamePopup.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        SaveGamePopup?.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
