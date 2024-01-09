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

    [Export]
    public GoBackButton GoBack{get; set;} = null!;
    [Export]
    public ConfirmationDialog GoBackConfirmationDialog{get; set;} = null!;
    [Export]
    public Label LobbyIdLabel{get; set;} = null!;
    [Export]
    public Label Player1NameLabel{get; set;} = null!;
    [Export]
    public Label Player2NameLabel{get; set;} = null!;
    [Export]
    public LobbyGameChallengeMenu GameChallengeSubMenu{get; set;} = null!;

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
        if(LobbyIdLabel is not null)
        {
            LobbyIdLabel.Text = id.ToString();
        }
    }

    public void SetPlayer1Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        if(Player1NameLabel is not null)
        {
            Player1NameLabel.Text = name;
        }
    }

    public void SetPlayer2Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        if(Player2NameLabel is not null)
        {
            Player2NameLabel.Text = name;
        }
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
