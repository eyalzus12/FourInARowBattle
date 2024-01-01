using Godot;
using System;

namespace FourInARowBattle;

public partial class TokenBase : Node2D
{
    [Signal]
    public delegate void TokenFinishedDropEventHandler();

    [Export(PropertyHint.MultilineText)]
    public string TokenDescription{get; set;} = "NO DESCRIPTION SET FOR THIS TOKEN";
    [Export]
    public float TokenSpeed{get; set;} = 60f;
    [Export]
    public float TokenAcceleration{get; set;} = 10f;

    private Color _tokenColor = Colors.White;
    public Color TokenColor
    {
        get => _tokenColor;
        set
        {
            _tokenColor = value;
            Modulate = value;
        }
    }

    public virtual GameResultEnum Result
    {
        get
        {
            if(TokenColor == Colors.Red) return GameResultEnum.Player1Win;
            if(TokenColor == Colors.Blue) return GameResultEnum.Player2Win;
            return GameResultEnum.None;
        }
    }

    protected int Row{get; private set;}
    protected int Col{get; private set;}
    protected Board Board{get; private set;} = null!;

    public Vector2? DesiredPosition{get; set;} = null;
    private float _currentSpeed;
    public bool ActivatedPower{get; set;} = false;

    public virtual bool SameAs(TokenBase? t) => t is not null && TokenColor == t.TokenColor;

    public override void _Ready()
    {
        _currentSpeed = 0;
        //avoid overriding previous modulate
        if(Modulate == Colors.White)
            Modulate = TokenColor;
    }

    public virtual void TokenSpawn(Board board, int row, int col)
    {
        Row = row;
        Col = col;
        Board = board;
    }

    public virtual void LocationChanged(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public virtual void OnDropFinished()
    {

    }

    public bool FinishedDrop{get
    {
        if(DesiredPosition is null) return true;
        Vector2 _desired = (Vector2)DesiredPosition;
        return _desired.IsEqualApprox(GlobalPosition);
    }}

    public override void _PhysicsProcess(double delta)
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if(DesiredPosition is null) return;

        Vector2 _desired = (Vector2)DesiredPosition;
        if(!_desired.IsEqualApprox(GlobalPosition))
        {
            _currentSpeed = Mathf.MoveToward(_currentSpeed, TokenSpeed, TokenAcceleration);
            GlobalPosition = GlobalPosition.MoveToward(_desired, _currentSpeed);
        }
        else
        {
            if(!ActivatedPower) OnDropFinished();
            ActivatedPower = true;
            EmitSignal(SignalName.TokenFinishedDrop);
            DesiredPosition = null;
            _currentSpeed = 0;

            //sound
            Autoloads.AudioManager.PlayersPool
                .GetObject()
                .Play(Autoloads.GlobalResources.TEST_LAND);
        }
    }

    /*public Action? TweenFinishedAction{get; set;}
    public void ConnectTweenFinished()
    {
        if(TweenFinishedAction is null) return;
        if(!TokenTween.IsInstanceValid()) TokenTween = null;
        if(TokenTween is not null)
        {
            //bind to current tween
            Tween bind = TokenTween;

            TokenTween.Finished += () =>
            {
                if(TweenFinishedAction is not null)
                    TweenFinishedAction();
                TweenFinishedAction = null;

                //make sure old tween doesn't destroy new one
                if(TokenTween == bind)
                {
                    TokenTween?.Kill();
                    TokenTween?.Dispose();
                    TokenTween = null;
                }
            };
        }
        else
            TweenFinishedAction();
    }*/

    public virtual void DeserializeFrom(Board board, TokenData data)
    {
        TokenColor = data.TokenColor;
        Modulate = data.TokenModulate;
        GlobalPosition = data.GlobalPosition;
        DesiredPosition = float.IsNaN(data.DesiredPosition.X) ? null : data.DesiredPosition;
    }

    public virtual TokenData SerializeTo() => new()
    {
        TokenScenePath = SceneFilePath,
        TokenColor = TokenColor,
        TokenModulate = Modulate,
        GlobalPosition = GlobalPosition,
        DesiredPosition = DesiredPosition ?? new Vector2(float.NaN, float.NaN)
    };
}
