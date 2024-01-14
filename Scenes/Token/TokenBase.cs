using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// The base class for all tokens
/// </summary>
public partial class TokenBase : Node2D
{
    /// <summary>
    /// Token finished dropping
    /// </summary>
    [Signal]
    public delegate void TokenFinishedDropEventHandler();

    /// <summary>
    /// The description of the token type. To be set in the editor.
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string TokenDescription{get; private set;} = "NO DESCRIPTION SET FOR THIS TOKEN";
    /// <summary>
    /// The maximum drop speed of the token
    /// </summary>
    [Export]
    private float _tokenSpeed = 30f;
    /// <summary>
    /// The drop acceleration of the token
    /// </summary>
    [Export]
    private float _tokenAcceleration = 5f;

    private Color _tokenColor = Colors.White;
    /// <summary>
    /// The token color
    /// </summary>
    public Color TokenColor
    {
        get => _tokenColor;
        set
        {
            _tokenColor = value;
            Modulate = value;
        }
    }

    /// <summary>
    /// What game result (player) the token represents
    /// </summary>
    public virtual GameResultEnum Result
    {
        get
        {
            if(TokenColor == Colors.Red) return GameResultEnum.PLAYER1_WIN;
            if(TokenColor == Colors.Blue) return GameResultEnum.PLAYER2_WIN;
            return GameResultEnum.NONE;
        }
    }

    /// <summary>
    /// The token row
    /// </summary>
    protected int Row{get; private set;}
    /// <summary>
    /// The token column
    /// </summary>
    protected int Col{get; private set;}
    /// <summary>
    /// The token board
    /// </summary>
    protected Board Board{get; private set;} = null!;

    /// <summary>
    /// The position the token wants to get to
    /// </summary>
    public Vector2? DesiredPosition{get; set;} = null;
    /// <summary>
    /// The token's current speed
    /// </summary>
    private float _currentSpeed;
    /// <summary>
    /// Whether the token activated its power already
    /// </summary>
    public bool ActivatedPower{get; set;} = false;

    /// <summary>
    /// Check whether the token is the same team as another
    /// </summary>
    /// <param name="t">The other token</param>
    /// <returns>Whether they are the same</returns>
    public virtual bool SameAs(TokenBase? t) => t is not null && TokenColor == t.TokenColor;

    public override void _Ready()
    {
        //reset values to ensure proper initialization when out of scene pool
        Row = -1;
        Col = -1;
        Board = null!;
        DesiredPosition = null;
        _currentSpeed = 0;
        ActivatedPower = false;

        //avoid overriding previous modulate
        if(Modulate == Colors.White)
            Modulate = TokenColor;
    }

    /// <summary>
    /// Called when a token spawns into the board
    /// </summary>
    /// <param name="board">The board</param>
    /// <param name="row">The row</param>
    /// <param name="col">The column</param>
    public virtual void TokenSpawn(Board board, int row, int col)
    {
        ArgumentNullException.ThrowIfNull(board);
        Row = row;
        Col = col;
        Board = board;
    }

    /// <summary>
    /// Called when the location of the token inside the board changes
    /// </summary>
    /// <param name="row">The new row</param>
    /// <param name="col">The new column</param>
    public virtual void LocationChanged(int row, int col)
    {
        Row = row;
        Col = col;
    }

    /// <summary>
    /// Called when the token finished dropping
    /// </summary>
    public virtual void OnDropFinished()
    {

    }

    /// <summary>
    /// Whether the token finished dropping
    /// </summary>
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

    /// <summary>
    /// Update the token position
    /// </summary>
    private void UpdatePosition()
    {
        if(DesiredPosition is null) return;

        Vector2 _desired = (Vector2)DesiredPosition;
        //not at the location yet
        if(!_desired.IsEqualApprox(GlobalPosition))
        {
            //increase speed by acceleration, clamping at max speed
            _currentSpeed = Mathf.MoveToward(_currentSpeed, _tokenSpeed, _tokenAcceleration);
            //alter position by speed
            GlobalPosition = GlobalPosition.MoveToward(_desired, _currentSpeed);
        }
        //reached spot
        else
        {
            //we null first because OnDropFinished and the four-in-a-row checks might change DesiredPosition
            DesiredPosition = null;
            EmitSignal(SignalName.TokenFinishedDrop);
            //no power activated yet. activate.
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
                    .Play(Autoloads.GlobalResources.TOKEN_LAND_SOUND);
            }
            
            _currentSpeed = 0;
        }
    }

    /// <summary>
    /// Load token data
    /// </summary>
    /// <param name="board">The token board</param>
    /// <param name="data">The token data</param>
    public virtual void DeserializeFrom(Board board, TokenData data)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(data);
        TokenColor = data.TokenColor;
        Modulate = data.TokenModulate;
        GlobalPosition = data.GlobalPosition;
        DesiredPosition = float.IsNaN(data.DesiredPosition.X) ? null : data.DesiredPosition;
    }

    /// <summary>
    /// Save current token state
    /// </summary>
    /// <returns>The token state</returns>
    public virtual TokenData SerializeTo() => new()
    {
        TokenScenePath = SceneFilePath,
        TokenColor = TokenColor,
        TokenModulate = Modulate,
        GlobalPosition = GlobalPosition,
        DesiredPosition = DesiredPosition ?? new Vector2(float.NaN, float.NaN)
    };
}
