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

    [Export]
    public Label ChallengeStatusLabel{get; set;} = null!;
    [Export]
    public Button SendChallengeButton{get; set;} = null!;
    [Export]
    public Button CancelChallengeButton{get; set;} = null!;
    [Export]
    public Button AcceptChallengeButton{get; set;} = null!;
    [Export]
    public Button RejectChallengeButton{get; set;} = null!;

    private void VerifyExports()
    {
        if(ChallengeStatusLabel is null) { GD.PushError($"No {nameof(ChallengeStatusLabel)} set"); return; }
        if(SendChallengeButton is null) { GD.PushError($"No {nameof(SendChallengeButton)} set"); return; }
        if(CancelChallengeButton is null) { GD.PushError($"No {nameof(CancelChallengeButton)} set"); return; }
        if(AcceptChallengeButton is null) { GD.PushError($"No {nameof(AcceptChallengeButton)} set"); return; }
        if(RejectChallengeButton is null) { GD.PushError($"No {nameof(RejectChallengeButton)} set"); return; }
    }

    private void ConnectSignals()
    {
        SendChallengeButton.Pressed += OnSendChallengeButtonPressed;
        CancelChallengeButton.Pressed += OnCancelChallengeButtonPressed;
        AcceptChallengeButton.Pressed += OnAcceptChallengeButtonPressed;
        RejectChallengeButton.Pressed += OnRejectChallengeButtonPressed;
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
        SendChallengeButton.Visible = true;
        CancelChallengeButton.Visible = false;
        AcceptChallengeButton.Visible = false;
        RejectChallengeButton.Visible = false;
    }

    public void SetState_SentChallenge()
    {
        SendChallengeButton.Visible = false;
        CancelChallengeButton.Visible = true;
        AcceptChallengeButton.Visible = false;
        RejectChallengeButton.Visible = false;
    }

    public void SetState_GotChallenge()
    {
        SendChallengeButton.Visible = false;
        CancelChallengeButton.Visible = false;
        AcceptChallengeButton.Visible = true;
        RejectChallengeButton.Visible = true;
    }

    public void SetState_ChallengeAccepted()
    {
        SendChallengeButton.Visible = false;
        CancelChallengeButton.Visible = false;
        AcceptChallengeButton.Visible = false;
        RejectChallengeButton.Visible = false;
    }
}
