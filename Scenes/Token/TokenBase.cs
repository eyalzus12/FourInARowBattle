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
    public float TokenSpeed{get; set;} = 30f;
    [Export]
    public float TokenAcceleration{get; set;} = 5f;

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
        ActivatedPower = false;
        //avoid overriding previous modulate
        if(Modulate == Colors.White)
            Modulate = TokenColor;
    }

    public virtual void TokenSpawn(Board board, int row, int col)
    {
        ArgumentNullException.ThrowIfNull(board);
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
            //we null first because OnDropFinished and the four-in-a-row checks might change DesiredPosition
            DesiredPosition = null;
            //we before OnDropFinished first in order to trigger the four-in-a-row check before OnDropFinished might cause changes
            EmitSignal(SignalName.TokenFinishedDrop);

            if(!ActivatedPower)
            {
                ActivatedPower = true;
                OnDropFinished();
            }

            //sound. avoid playing if speed is 0, which can be caused if desired position and position are set to the same thing.
            if(_currentSpeed != 0)
            {
                Autoloads.AudioManager.AudioPlayersPool
                    .GetObject()
                    .Play(Autoloads.GlobalResources.TEST_LAND);
            }
            
            _currentSpeed = 0;
        }
    }

    public virtual void DeserializeFrom(Board board, TokenData data)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(data);
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
