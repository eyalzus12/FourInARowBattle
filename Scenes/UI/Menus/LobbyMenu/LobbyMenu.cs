using Godot;
using System;

namespace FourInARowBattle;

public partial class LobbyMenu : Control
{
    [Signal]
    public delegate void ExitLobbyRequestedEventHandler(string path);

    [Export]
    public GoBackButton? GoBack{get; set;}
    [Export]
    public ConfirmationDialog? GoBackConfirmationDialog{get; set;}
    [Export]
    public Label? LobbyIdLabel{get; set;}
    [Export]
    public Label? Player1NameLabel{get; set;}
    [Export]
    public Label? Player2NameLabel{get; set;}

    private string? _goBackRequestPath;

    public override void _Ready()
    {
        if(GoBackConfirmationDialog is not null)
        {
            GoBackConfirmationDialog.Confirmed += () =>
            {
                if(_goBackRequestPath is null) return;
                EmitSignal(SignalName.ExitLobbyRequested, _goBackRequestPath);
            };

            GetWindow().SizeChanged += () =>
            {
                if(GoBackConfirmationDialog.Visible)
                    OnGoBackButtonChangeSceneRequested(_goBackRequestPath!);
            };
        }

        if(GoBack is not null)
        {
            GoBack.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        }
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        _goBackRequestPath = path;

        //Vector2I decorations = GetWindow().GetSizeOfDecorations();
        GoBackConfirmationDialog?.PopupCentered(/*GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y)*/);
    }

    public void SetLobbyId(uint id)
    {
        if(LobbyIdLabel is not null)
        {
            LobbyIdLabel.Text = id.ToString();
        }
    }

    public void SetPlayer1Name(string name)
    {
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        if(Player1NameLabel is not null)
        {
            Player1NameLabel.Text = name;
        }
    }

     public void SetPlayer2Name(string name)
    {
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        if(Player2NameLabel is not null)
        {
            Player2NameLabel.Text = name;
        }
    }
}
