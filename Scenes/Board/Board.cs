using Godot;
using System;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class Board : Node2D
{
    [Signal]
    public delegate void ScoreIncreasedEventHandler(GameTurnEnum who, int amount);
    [Signal]
    public delegate void TokenPlacedEventHandler(TokenBase who, int row, int col);
    [Signal]
    public delegate void TokenStartedDropEventHandler();
    [Signal]
    public delegate void TokenFinishedDropEventHandler();

    [ExportCategory("Nodes")]
    [Export]
    private Control _boardBase = null!;
    [ExportCategory("")]
    [Export]
    public int Rows{get; set;} = 6;
    [Export]
    public int Columns{get; set;} = 7;
    [Export]
    private int _winRequirement = 4;
    [Export]
    private float _leftMargin = 32;
    [Export]
    private float _rightMargin = 32;
    [Export]
    private float _topMargin = 32;
    [Export]
    private float _bottomMargin = 32;
    [Export]
    public float SlotRadius{get; set;} = 24;
    [Export]
    private float _tokenRadius = 21;
    [Export]
    private float _dropStartOffset = 500;
    [Export]
    private float _ghostTokenAlpha = 0.5f;
    [Export]
    private Texture2D _holeMaskTexture = null!;

    private Vector2 HoleScale => 2 * SlotRadius * Vector2.One / _holeMaskTexture.GetSize();
    private Vector2 TokenScale => 2 * _tokenRadius * Vector2.One / _holeMaskTexture.GetSize();

    private TokenBase?[,] _tokenGrid = null!;

    private Node2D? _maskGroup = null;

    private Vector2 BoardPosition => _boardBase.GlobalPosition + new Vector2(_leftMargin,_topMargin);
    private Vector2 BoardSize => _boardBase.Size - new Vector2(_rightMargin,_bottomMargin);

    private Vector2 HoleJump => BoardSize / new Vector2(Columns+1, Rows+1);
    private Vector2 CenterOffset => SlotRadius * Vector2.One;
    public Vector2 HolePosition(int row, int col) => BoardPosition + HoleJump*new Vector2(col,row) - CenterOffset;

    private readonly record struct GhostTokenRenderData(Texture2D TokenTexture, Color TokenColor, int Column){}
    private GhostTokenRenderData? _ghostToken;

    private readonly HashSet<TokenBase> _droppingTokens = new();

    private void AddDroppingToken(TokenBase token)
    {
        if(_droppingTokens.Count == 0) EmitSignal(SignalName.TokenStartedDrop);
        _droppingTokens.Add(token);
        token.TreeExiting += () => RemoveDroppingToken(token);
        token.TokenFinishedDrop += () => RemoveDroppingToken(token);
    }

    private void RemoveDroppingToken(TokenBase token)
    {
        _droppingTokens.Remove(token);
        if(_droppingTokens.Count == 0)
        {
            EmitSignal(SignalName.TokenFinishedDrop);
            DecideResult();
        }
    }

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_boardBase);
    }

    public override void _Ready()
    {
        _tokenGrid ??= new TokenBase?[Rows,Columns];
        CreateHoleMasks();
    }

    public override void _Draw()
    {
        if(_ghostToken is not null)
        {
            GhostTokenRenderData ghostToken = (GhostTokenRenderData)_ghostToken;
            int? _row = FindTopSpot(ghostToken.Column);
            if(_row is not null)
            {
                int row = (int)_row;
                Vector2 center = ToLocal(HolePosition(row+1,ghostToken.Column+1));
                Vector2 newsize = 2 * _tokenRadius * Vector2.One;
                Vector2 newstart = center - newsize/2;
                DrawTextureRect(
                    ghostToken.TokenTexture,
                    new Rect2(newstart, newsize),
                    false,
                    ghostToken.TokenColor with {A = _ghostTokenAlpha}
                );
            }
        }
    }

    private void CreateHoleMasks()
    {
        _maskGroup?.QueueFree();
        //add holes
        _maskGroup = new(){Material = new CanvasItemMaterial(){BlendMode = CanvasItemMaterial.BlendModeEnum.Sub}};
        //use AddSibling to ensure correct node order
        _boardBase.AddSibling(_maskGroup);
        //add masks
        for(int row = 1; row <= Rows; ++row)
        {
            for(int col = 1; col <= Columns; ++col)
            {
                Sprite2D holeMask = new(){Texture = _holeMaskTexture, UseParentMaterial = true, Scale = HoleScale};
                _maskGroup.AddChild(holeMask);
                holeMask.GlobalPosition = HolePosition(row,col);
            }
        }
    }

    public bool AddToken(int col, TokenBase t)
    {
        if(!t.IsInstanceValid()) return false;

        int? _row = FindTopSpot(col);
        if(_row is null)
        {
            Autoloads.ScenePool.ReturnScene(t);
            return false;
        }
        int row = (int)_row;

        t.Scale = TokenScale;
        t.RequestReady();
        AddChild(t);
        _tokenGrid[row,col] = t;
        t.TokenSpawn(this, row, col);
        Vector2 desired = HolePosition(row + 1, col + 1);
        t.DesiredPosition = desired;
        t.GlobalPosition = desired + Vector2.Up * _dropStartOffset;
        AddDroppingToken(t);
        EmitSignal(SignalName.TokenPlaced, t, row, col);
        QueueRedraw();
        return true;
    }

    public void RenderGhostToken(Texture2D texture, Color color, int col)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _ghostToken = new(texture,color,col);
        QueueRedraw();
    }

    public void HideGhostToken()
    {
        _ghostToken = null;
        QueueRedraw();
    }

    public GameResultEnum DecideResult()
    {
        List<(int,int)> toRemove = new();
        Dictionary<GameResultEnum, int> resultCounts = new();
        bool haveFree = false;
        for(int row = 0; row < Rows; ++row)
        {
            for(int col = 0; col < Columns; ++col)
            {
                if(_tokenGrid[row,col] is not null)
                    CheckSpotWin(row, col, resultCounts, toRemove);
                else
                    haveFree = true;
            }
        }
        if(!haveFree) return GameResultEnum.DRAW;

        foreach((GameResultEnum result, int count) in resultCounts)
        {
            if(count != 0)
            {
                //the counters work by player turn
                //so we need to convert the result to the turn
                //for non-player results we just give a nonexistent turn value
                GameTurnEnum resultTurn = result.GameResultToGameTurn();
                EmitSignal(SignalName.ScoreIncreased, (int)resultTurn, count);
            }
        }

        List<TokenBase> tokensToRemove = new(toRemove.Count);
        
        foreach((int row, int col) in toRemove)
        {
            TokenBase? t = _tokenGrid[row,col];
            _tokenGrid[row,col] = null;
            if(t.IsInstanceValid())
                tokensToRemove.Add(t);
        }

        foreach(TokenBase t in tokensToRemove)
        {
            if(t.IsInstanceValid())
            {
                Autoloads.ScenePool.ReturnScene(t);
            }
        }
        
        if(toRemove.Count > 0)
        {
            ApplyGravity();
            //after applying gravity we might have new wins
            //so call recursively
            DecideResult();
            QueueRedraw();
        }

        return GameResultEnum.NONE;
    }

    private void CheckSpotWin(int row, int col, Dictionary<GameResultEnum, int> resultCounts, List<(int,int)> toRemove)
    {
        ArgumentNullException.ThrowIfNull(resultCounts);
        ArgumentNullException.ThrowIfNull(toRemove);

        TokenBase? token = _tokenGrid[row,col];
        if(!token.IsInstanceValid()) _tokenGrid[row,col] = token = null;
        if(token is null || token.Result == GameResultEnum.NONE) return;
        //if(!token.FinishedDrop) return;

        bool foundWin = false;
        List<(int,int)> currentTokenStreak = new(_winRequirement-1);
        if(!resultCounts.ContainsKey(token!.Result)) resultCounts[token.Result] = 0;

        //check up
        if(row >= _winRequirement-1)
        {
            bool upFail = false;
            for(int rowOffset = 1; rowOffset < _winRequirement; rowOffset++)
                if(!token.SameAs(_tokenGrid[row-rowOffset,col]))
                {
                    upFail = true;
                    currentTokenStreak.Clear();
                    break;
                }
                else
                {
                    currentTokenStreak.Add((row-rowOffset,col));
                }
            if(!upFail)
            {
                resultCounts[token.Result]++;
                toRemove.AddRange(currentTokenStreak);
                currentTokenStreak.Clear();
                foundWin = true;
            }
        }
        //check left
        if(col >= _winRequirement-1)
        {
            bool leftFail = false;
            for(int colOffset = 1; colOffset < _winRequirement; colOffset++)
                if(!token.SameAs(_tokenGrid[row,col-colOffset]))
                {
                    leftFail = true;
                    currentTokenStreak.Clear();
                    break;
                }
                else
                {
                    currentTokenStreak.Add((row,col-colOffset));
                }
            if(!leftFail)
            {
                resultCounts[token.Result]++;
                toRemove.AddRange(currentTokenStreak);
                currentTokenStreak.Clear();
                foundWin = true;
            }
        }
        //check up left
        if(row >= _winRequirement-1 && col >= _winRequirement-1)
        {
            bool upLeftFail = false;
            for(int offset = 1; offset < _winRequirement; offset++)
                if(!token.SameAs(_tokenGrid[row-offset,col-offset]))
                {
                    upLeftFail = true;
                    currentTokenStreak.Clear();
                    break;
                }
                else
                {
                    currentTokenStreak.Add((row-offset,col-offset));
                }
            if(!upLeftFail)
            {
                resultCounts[token.Result]++;
                toRemove.AddRange(currentTokenStreak);
                currentTokenStreak.Clear();
                foundWin = true;
            }
        }
        //check up right
        if(row >= _winRequirement-1 && col <= Columns-_winRequirement)
        {
            bool upRightFail = false;
            for(int offset = 1; offset < _winRequirement; offset++)
                if(!token.SameAs(_tokenGrid[row-offset,col+offset]))
                {
                    upRightFail = true;
                    currentTokenStreak.Clear();
                    break;
                }
                else
                {
                    currentTokenStreak.Add((row-offset,col+offset));
                }
            if(!upRightFail)
            {
                resultCounts[token.Result]++;
                toRemove.AddRange(currentTokenStreak);
                currentTokenStreak.Clear();
                foundWin = true;
            }
        }

        if(foundWin) toRemove.Add((row,col));
    }

    public int? FindTopSpot(int col)
    {
        int row = 0;
        for(; row < Rows; row++)
        {
            if(_tokenGrid[row,col].IsInstanceValid()) break;
            _tokenGrid[row,col] = null;
        }
        if(row == 0) return null;
        return row-1;
    }

    public int? FindBottomSpot(int col)
    {
        int row = Rows-1;
        for(; row >= 0; row--)
        {
            if(_tokenGrid[row,col].IsInstanceValid()) break;
            _tokenGrid[row,col] = null;
        }
        if(row == Rows-1) return null;
        return row+1;
    }

    public void ApplyGravity()
    {
        for(int col = 0; col < Columns; ++col) ApplyColGravity(col);
    }

    private readonly HashSet<int> _colGravityLock = new();
    public void ApplyColGravity(int col)
    {
        if(_colGravityLock.Contains(col)) return;
        _colGravityLock.Add(col);
        List<TokenBase> tokens = new();
        for(int row = Rows-1; row >= 0; row--)
        {
            TokenBase? t = _tokenGrid[row,col];
            if(!t.IsInstanceValid())
                _tokenGrid[row,col] = null;
            else
                tokens.Add(t);
        }
        int tokenIdx = Rows-1;
        foreach(TokenBase t in tokens)
        {
            _tokenGrid[tokenIdx,col] = t;
            t.DesiredPosition = HolePosition(tokenIdx + 1, col + 1);
            t.LocationChanged(tokenIdx + 1, col + 1);
            AddDroppingToken(t);
            tokenIdx--;
        }
        for(; tokenIdx >= 0; tokenIdx--)
        {
            _tokenGrid[tokenIdx,col] = null;
        }
        _colGravityLock.Remove(col);
        QueueRedraw();
    }

    public void FlipCol(int col)
    {
        TokenBase?[] newCol = new TokenBase?[Rows];
        int newRow = Rows-1;
        for(int row = 0; row < Rows; ++row)
        {
            TokenBase? t = _tokenGrid[row,col];
            if(!t.IsInstanceValid()) t = _tokenGrid[row,col] = null;
            if(t is null) continue;
            newCol[newRow] = t;
            t.DesiredPosition = t.GlobalPosition = HolePosition(newRow + 1, col + 1);
            t.LocationChanged(newRow + 1, col + 1);
            --newRow;
        }
        for(int row = 0; row < Rows; ++row)
        {
            _tokenGrid[row,col] = (row <= newRow) ? null : newCol[row];
        }
    }

    public void FlipRow(int row)
    {
        TokenBase?[] newRow = new TokenBase?[Columns];
        for(int col = 0; col < Columns; ++col)
        {
            TokenBase? t = _tokenGrid[row,col];
            if(!t.IsInstanceValid()) _tokenGrid[row,col] = null;
        }
        for(int col = 0; col < Columns; ++col)
        {
            TokenBase? t = _tokenGrid[row,col];
            newRow[Columns-1-col] = t;
            if(t is not null)
            {
                t.DesiredPosition = t.GlobalPosition = HolePosition(row + 1, Columns - col);
                t.LocationChanged(row + 1, Columns - col);
            }
        }
        for(int col = 0; col < Columns; ++col)
        {
            _tokenGrid[row,col] = newRow[col];
        }
    }

    public void FlipVertical()
    {
        //i,j -> Rows-1-i,j
        TokenBase?[,] newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < Rows; ++row)
            for(int col = 0; col < Columns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                if(!t.IsInstanceValid()) _tokenGrid[row,col] = null;
            }
        for(int row = 0; row < Rows; ++row)
            for(int col = 0; col < Columns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                newGrid[Rows-1-row,col] = t;
                if(t is not null)
                {
                    t.DesiredPosition = t.GlobalPosition = HolePosition(Rows - row, col + 1);
                    t.LocationChanged(Rows - row, col + 1);
                }
            }
        _tokenGrid = newGrid;
    }

    public void RotateLeft()
    {
        //i,j -> Columns-1-j,i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        _boardBase.Position = new Vector2(_boardBase.Position.Y, _boardBase.Position.X);
        _boardBase.Size = new Vector2(_boardBase.Size.Y, _boardBase.Size.X);
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                if(!t.IsInstanceValid()) _tokenGrid[row,col] = null;
            }
        //swap
        (Rows,Columns) = (Columns,Rows);
        //go over grid
        TokenBase?[,] newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                newGrid[oldColumns-1-col,row] = t;
                if(t is not null)
                {
                    t.DesiredPosition = t.GlobalPosition = HolePosition(oldColumns - col, row + 1);
                    t.LocationChanged(oldColumns - col, row + 1);
                }
            }
        _tokenGrid = newGrid;
        CreateHoleMasks();
    }

    public void RotateRight()
    {
        //i,j -> j,Rows-1-i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        _boardBase.Position = new Vector2(_boardBase.Position.Y, _boardBase.Position.X);
        _boardBase.Size = new Vector2(_boardBase.Size.Y, _boardBase.Size.X);
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                if(!t.IsInstanceValid()) _tokenGrid[row,col] = null;
            }
        //swap
        (Rows,Columns) = (Columns,Rows);
        //go over grid
        TokenBase?[,] newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = _tokenGrid[row,col];
                newGrid[col,oldRows-1-row] = t;
                if(t is not null)
                {
                    t.DesiredPosition = t.GlobalPosition = HolePosition(col + 1, oldRows - row);
                    t.LocationChanged(col + 1, oldRows - row);
                }
            }
        _tokenGrid = newGrid;
        CreateHoleMasks();
    }

    public void RemoveToken(int row, int col)
    {
        TokenBase? t = _tokenGrid[row,col];
        _tokenGrid[row,col] = null;
        if(t.IsInstanceValid())
        {
            Autoloads.ScenePool.ReturnScene(t);
        }
    }

    public virtual void DeserializeFrom(BoardData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        if(data.Grid.Count != data.Rows)
        {
            GD.PushError($"Board data has row count of {data.Rows}, but its grid has {data.Grid.Count} rows");
            return;
        }

        //cleanup
        _droppingTokens.Clear();
        EmitSignal(SignalName.TokenFinishedDrop);
        _ghostToken = null;
        //if deserialize is called before _Ready, this makes sure we don't access a null grid
        if(_tokenGrid is not null)
        {
            for(int row = 0; row < Rows; ++row)
            {
                for(int col = 0; col < Columns; ++col)
                {
                    TokenBase? t = _tokenGrid[row,col];
                    _tokenGrid[row,col] = null;
                    if(t.IsInstanceValid())
                        Autoloads.ScenePool.ReturnScene(t);
                }
            }
        }

        Rows = data.Rows;
        Columns = data.Columns;
        _boardBase.Position = data.BoardPosition;
        _boardBase.Size = data.BoardSize;
        _winRequirement = data.WinRequirement;
        CreateHoleMasks();

        _tokenGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < Rows; ++row)
        {
            if(data.Grid[row].Count != data.Columns)
            {
                GD.PushError($"Board data has column count of {data.Columns}, but its {row}th row has {data.Grid[row].Count} elements");
                return;
            }
            for(int col = 0; col < Columns; ++col)
            {
                TokenData? tdata = data.Grid[row][col];
                if(tdata is null) continue;
                PackedScene scene = ResourceLoader.Load<PackedScene>(tdata.TokenScenePath);
                TokenBase t = Autoloads.ScenePool.GetScene<TokenBase>(scene);
                _tokenGrid[row,col] = t;
                t.Scale = TokenScale;
                AddChild(t);
                t.DeserializeFrom(this, tdata);
                t.TokenSpawn(this, row, col);
                t.DesiredPosition = null;
            }
        }
    }

    public virtual BoardData SerializeTo()
    {
        BoardData data = new()
        {
            Rows = Rows,
            Columns = Columns,
            WinRequirement = _winRequirement,
            Grid = new(),
            BoardPosition = _boardBase.Position,
            BoardSize = _boardBase.Size
        };
        for(int row = 0; row < Rows; ++row)
        {
            data.Grid.Add(new Godot.Collections.Array<TokenData?>());
            for(int col = 0; col < Columns; ++col)
            {
                data.Grid[row].Add(_tokenGrid[row,col]?.SerializeTo());
            }
        }
        return data;
    }
}
