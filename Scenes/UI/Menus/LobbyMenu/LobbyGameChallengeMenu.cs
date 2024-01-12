using Godot;
using System;

namespace FourInARowBattle;

public partial class LobbyGameChallengeMenu : Control
{
    #region Signals
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
    private Button _sendChallengeButton = null!;
    [Export]
    private Button _cancelChallengeButton = null!;
    [Export]
    private Button _acceptChallengeButton = null!;
    [Export]
    private Button _rejectChallengeButton = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_sendChallengeButton);
        ArgumentNullException.ThrowIfNull(_cancelChallengeButton);
        ArgumentNullException.ThrowIfNull(_acceptChallengeButton);
        ArgumentNullException.ThrowIfNull(_rejectChallengeButton);
    }

    private void ConnectSignals()
    {
        _sendChallengeButton.Pressed += OnSendChallengeButtonPressed;
        _cancelChallengeButton.Pressed += OnCancelChallengeButtonPressed;
        _acceptChallengeButton.Pressed += OnAcceptChallengeButtonPressed;
        _rejectChallengeButton.Pressed += OnRejectChallengeButtonPressed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    #region Signal Handling
    private void OnSendChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeSent);
    }

    private void OnCancelChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeCanceled);
    }

    private void OnAcceptChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeAccepted);
    }

    private void OnRejectChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeRejected);
    }
    #endregion

    public void SetState_NoChallenge()
    {
        _sendChallengeButton.Visible = true;
        _cancelChallengeButton.Visible = false;
        _acceptChallengeButton.Visible = false;
        _rejectChallengeButton.Visible = false;
    }

    public void SetState_SentChallenge()
    {
        _sendChallengeButton.Visible = false;
        _cancelChallengeButton.Visible = true;
        _acceptChallengeButton.Visible = false;
        _rejectChallengeButton.Visible = false;
    }

    public void SetState_GotChallenge()
    {
        _sendChallengeButton.Visible = false;
        _cancelChallengeButton.Visible = false;
        _acceptChallengeButton.Visible = true;
        _rejectChallengeButton.Visible = true;
    }

    public void SetState_CannotChallenge()
    {
        _sendChallengeButton.Visible = false;
        _cancelChallengeButton.Visible = false;
        _acceptChallengeButton.Visible = false;
        _rejectChallengeButton.Visible = false;
    }
}
