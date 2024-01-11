using Godot;
using System;

namespace FourInARowBattle;

public partial class LobbyMenu : Control
{
    #region Signals
    [Signal]
    public delegate void ExitLobbyRequestedEventHandler(string path);
    [Signal]
    public delegate void ChallengeSentEventHandler();
    [Signal]
    public delegate void ChallengeCanceledEventHandler();
    [Signal]
    public delegate void ChallengeAcceptedEventHandler();
    [Signal]
    public delegate void ChallengeRejectedEventHandler();
    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private GoBackButton GoBack = null!;
    [Export]
    private ConfirmationDialog GoBackConfirmationDialog = null!;
    [Export]
    private Label LobbyIdLabel = null!;
    [Export]
    private Label Player1NameLabel = null!;
    [Export]
    private Label Player2NameLabel = null!;
    [Export]
    private LobbyGameChallengeMenu GameChallengeSubMenu = null!;

    private string? _goBackRequestPath;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(GoBack);
        ArgumentNullException.ThrowIfNull(GoBackConfirmationDialog);
        ArgumentNullException.ThrowIfNull(LobbyIdLabel);
        ArgumentNullException.ThrowIfNull(Player1NameLabel);
        ArgumentNullException.ThrowIfNull(Player2NameLabel);
        ArgumentNullException.ThrowIfNull(GameChallengeSubMenu);
    }

    private void ConnectSignals()
    {
        GoBackConfirmationDialog.Confirmed += OnGoBackConfirmationDialogConfirmed;
        GetWindow().SizeChanged += OnWindowSizeChanged;
        GoBack.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        GameChallengeSubMenu.ChallengeSent += OnChallengeSubMenuChallengeSent;
        GameChallengeSubMenu.ChallengeCanceled += OnChallengeSubMenuChallengeCanceled;
        GameChallengeSubMenu.ChallengeAccepted += OnChallengeSubMenuChallengeAccepted;
        GameChallengeSubMenu.ChallengeRejected += OnChallengeSubMenuChallengeRejected;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    public bool LobbyFull() => Player1NameLabel.Text != "" && Player2NameLabel.Text != "";

    #region Signal Handling
    private void OnGoBackConfirmationDialogConfirmed()
    {
        if(_goBackRequestPath is null) return;
        EmitSignal(SignalName.ExitLobbyRequested, _goBackRequestPath);
    }

    private void OnWindowSizeChanged()
    {
        if(GoBackConfirmationDialog.Visible)
            OnGoBackButtonChangeSceneRequested(_goBackRequestPath!);
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _goBackRequestPath = path;

        //Vector2I decorations = GetWindow().GetSizeOfDecorations();
        GoBackConfirmationDialog.PopupCentered(/*GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y)*/);
    }

    private void OnChallengeSubMenuChallengeSent()
    {
        EmitSignal(SignalName.ChallengeSent);
    }

    private void OnChallengeSubMenuChallengeCanceled()
    {
        EmitSignal(SignalName.ChallengeCanceled);
    }

    private void OnChallengeSubMenuChallengeAccepted()
    {
        EmitSignal(SignalName.ChallengeAccepted);
    }

    private void OnChallengeSubMenuChallengeRejected()
    {
        EmitSignal(SignalName.ChallengeRejected);
    }

    #endregion

    public void SetLobbyId(uint id)
    {
        LobbyIdLabel.Text = id.ToString();
    }

    private Color? _player1OldModulate = null;
    private Color? _player2OldModulate = null;

    public void SetPlayer1Marked()
    {
        if(_player2OldModulate is not null)
        {
            Player2NameLabel.Modulate = (Color)_player2OldModulate;
            _player2OldModulate = null;
        }

        _player1OldModulate ??= Player1NameLabel.Modulate;
        Player1NameLabel.Modulate = Colors.Blue;
    }

    public void SetPlayer2Marked()
    {
        if(_player1OldModulate is not null)
        {
            Player1NameLabel.Modulate = (Color)_player1OldModulate;
            _player1OldModulate = null;
        }

        _player2OldModulate ??= Player2NameLabel.Modulate;
        Player2NameLabel.Modulate = Colors.Blue;
    }

    public void ClearMark()
    {
        if(_player1OldModulate is not null)
        {
            Player1NameLabel.Modulate = (Color)_player1OldModulate;
            _player1OldModulate = null;
        }

        if(_player2OldModulate is not null)
        {
            Player2NameLabel.Modulate = (Color)_player2OldModulate;
            _player2OldModulate = null;
        }
    }

    public void SetPlayer1Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        Player1NameLabel.Text = name;
    }

    public void SetPlayer2Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        Player2NameLabel.Text = name;
    }

    public void SetChallengeState_NoChallenge()
    {
        GameChallengeSubMenu.SetState_NoChallenge();
    }

    public void SetChallengeState_SentChallenge()
    {
        GameChallengeSubMenu.SetState_SentChallenge();
    }

    public void SetChallengeState_GotChallenge()
    {
        GameChallengeSubMenu.SetState_GotChallenge();
    }

    public void SetChallengeState_CannotChallenge()
    {
        GameChallengeSubMenu.SetState_CannotChallenge();
    }
}
