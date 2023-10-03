using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Node2D
{
    public static readonly StringName TOKEN_TWEEN_META_NAME = "DropTween";

    [Signal]
    public delegate void TokenPlacedEventHandler(Board where, TokenBase who, int row, int col);

    [Export]
    public int Rows{get; set;} = 6;
    [Export]
    public int Columns{get; set;} = 7;
    [Export]
    public int WinRequirement{get; set;} = 4;
    [Export]
    public float LeftMargin{get; set;} = 32;
    [Export]
    public float RightMargin{get; set;} = 32;
    [Export]
    public float TopMargin{get; set;} = 32;
    [Export]
    public float BottomMargin{get; set;} = 32;
    [Export]
    public float SlotRadius{get; set;} = 24;
    [Export]
    public float TokenRadius{get; set;} = 21;
    [Export]
    public float DropStartOffset{get; set;} = 500;
    [Export]
    public float GhostTokenAlpha{get; set;} = 0.5f;
    [Export]
    public float TokenDropSpeed{get; set;} = 1000;
    [Export]
    public Texture2D HoleMaskTexture{get; set;} = null!;

    public Vector2 HoleScale => 2 * SlotRadius * Vector2.One / HoleMaskTexture.GetSize();
    public Vector2 TokenScale => 2 * TokenRadius * Vector2.One / HoleMaskTexture.GetSize();

    public TokenBase?[,] TokenGrid{get; set;} = null!;

    [Export]
    public Control BoardBase{get; set;} = null!;

    private Node2D? _maskGroup = null;

    public Vector2 BoardPosition => BoardBase.GlobalPosition + new Vector2(LeftMargin,TopMargin);
    public Vector2 BoardSize => BoardBase.Size - new Vector2(RightMargin,BottomMargin);

    private Vector2 HoleJump => BoardSize / new Vector2(Columns+1, Rows+1);
    private Vector2 CenterOffset => SlotRadius * Vector2.One;
    public Vector2 HolePosition(int row, int col) => BoardPosition + HoleJump*new Vector2(col,row) - CenterOffset;

    private readonly record struct GhostTokenRenderData(Texture2D TokenTexture, Color TokenColor, int Column){}
    private GhostTokenRenderData? _ghostToken;

    private EventBus _eventBus = null!;

    private readonly HashSet<TokenBase> _tweenedTokens = new();

    public override void _Ready()
    {
        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));
        TokenGrid = new TokenBase?[Rows,Columns];
        CreateHoleMasks();
    }

    public override void _Draw()
    {
        if(_ghostToken is not null)
        {
            var ghostToken = (GhostTokenRenderData)_ghostToken;
            var _row = FindTopSpot(ghostToken.Column);
            if(_row is not null)
            {
                var row = (int)_row;
                var start = HolePosition(row+1,ghostToken.Column+1) - CenterOffset;
                var size = 2 * SlotRadius * Vector2.One;
                var center = start + size/2;
                var newsize = size * TokenScale / HoleScale;
                var newstart = center - newsize/2;
                DrawTextureRect(
                    ghostToken.TokenTexture,
                    new Rect2(newstart, newsize),
                    false,
                    ghostToken.TokenColor with {A = GhostTokenAlpha}
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
        BoardBase.AddSibling(_maskGroup);
        //add masks
        for(int row = 1; row <= Rows; ++row)
        {
            for(int col = 1; col <= Columns; ++col)
            {
                Sprite2D holeMask = new(){Texture = HoleMaskTexture, UseParentMaterial = true, Scale = HoleScale};
                _maskGroup.AddChild(holeMask);
                holeMask.GlobalPosition = HolePosition(row,col);
            }
        }
    }

    public bool AddToken(int col, TokenBase t)
    {
        if(!IsInstanceValid(t)) return false;

        int? _row = FindTopSpot(col);
        if(_row is null)
        {
            t.QueueFree();
            return false;
        }
        int row = (int)_row;


        t.Scale = TokenScale;
        AddChild(t);
        var desiredPosition = ToLocal(HolePosition(row+1,col+1));
        TweenToken(t, desiredPosition + Vector2.Up * DropStartOffset, desiredPosition);
        TokenGrid[row,col] = t;
        t.OnPlace(this, row, col);
        EmitSignal(SignalName.TokenPlaced, this, t, row, col);
        QueueRedraw();
        return true;
    }

    public void RenderGhostToken(Texture2D texture, Color color, int col)
    {
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
                if(TokenGrid[row,col] is not null)
                    CheckSpotWin(row, col, resultCounts, toRemove);
                else
                    haveFree = true;
            }
        }
        if(!haveFree) return GameResultEnum.Draw;

        foreach(var (result,count) in resultCounts)
        {
            if(count != 0)
            {
                //the counters work by player turn
                //so we need to convert the result to the turn
                //for non-player results we just give a nonexistent turn value
                var resultTurn = result.GameResultToGameTurn();
                _eventBus.EmitSignal(EventBus.SignalName.ScoreIncreased, (int)resultTurn, count);
            }
        }

        foreach(var (row,col) in toRemove)
        {
            RemoveToken(row,col);
        }
        
        if(toRemove.Count > 0)
        {
            ApplyGravity();
            //after applying gravity we might have new wins
            //so call recursively
            DecideResult();
            QueueRedraw();
        }

        return GameResultEnum.None;
    }

    private void CheckSpotWin(int row, int col, Dictionary<GameResultEnum, int> resultCounts, List<(int,int)> toRemove)
    {
        TokenBase? token = TokenGrid[row,col];
        if(!IsInstanceValid(token)) TokenGrid[row,col] = token = null;
        if(token is null || token.Result == GameResultEnum.None) return;

        bool foundWin = false;
        List<(int,int)> currentTokenStreak = new(WinRequirement-1);
        if(!resultCounts.ContainsKey(token.Result)) resultCounts[token.Result] = 0;

        //check up
        if(row >= WinRequirement-1)
        {
            bool upFail = false;
            for(int rowOffset = 1; rowOffset < WinRequirement; rowOffset++)
                if(!token.SameAs(TokenGrid[row-rowOffset,col]))
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
        if(col >= WinRequirement-1)
        {
            bool leftFail = false;
            for(int colOffset = 1; colOffset < WinRequirement; colOffset++)
                if(!token.SameAs(TokenGrid[row,col-colOffset]))
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
        if(row >= WinRequirement-1 && col >= WinRequirement-1)
        {
            bool upLeftFail = false;
            for(int offset = 1; offset < WinRequirement; offset++)
                if(!token.SameAs(TokenGrid[row-offset,col-offset]))
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
        if(row >= WinRequirement-1 && col <= Columns-WinRequirement)
        {
            bool upRightFail = false;
            for(int offset = 1; offset < WinRequirement; offset++)
                if(!token.SameAs(TokenGrid[row-offset,col+offset]))
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
            if(IsInstanceValid(TokenGrid[row,col])) break;
            TokenGrid[row,col] = null;
        }
        if(row == 0) return null;
        return row-1;
    }

    public int? FindBottomSpot(int col)
    {
        int row = Rows-1;
        for(; row >= 0; row--)
        {
            if(IsInstanceValid(TokenGrid[row,col])) break;
            TokenGrid[row,col] = null;
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
            TokenBase? t = TokenGrid[row,col];
            if(t is null)
                continue;
            else if(!IsInstanceValid(t) || t.IsQueuedForDeletion())
                TokenGrid[row,col] = null;
            else
                tokens.Add(t);
        }
        int tokenIdx = Rows-1;
        foreach(var t in tokens)
        {
            TokenGrid[tokenIdx,col] = t;
            t.OnLocationUpdate(tokenIdx, col);
            TweenToken(t, t.Position, HolePosition(tokenIdx+1,col+1));
            tokenIdx--;
        }
        for(; tokenIdx >= 0; tokenIdx--)
        {
            TokenGrid[tokenIdx,col] = null;
        }
        _colGravityLock.Remove(col);
        QueueRedraw();
    }

    public void FlipCol(int col)
    {
        var tweenedList = new List<TokenBase>();
        var newCol = new TokenBase?[Rows];
        int newRow = Rows-1;
        for(int row = 0; row < Rows; ++row)
        {
            TokenBase? t = TokenGrid[row,col];
            //DisableTween(t);
            if(!IsInstanceValid(t)) t = TokenGrid[row,col] = null;
            if(t is null) continue;
            //has a valid tween
            if(
                t.TokenTween.IsInstanceValid() &&
                t.TokenTween.IsValid()
            )
            {
                tweenedList.Add(t);
            }
            //valid token. not moving.
            else
            {
                newCol[newRow] = t;
                t.OnLocationUpdate(newRow, col);
                t.GlobalPosition = HolePosition(newRow+1,col+1);
                --newRow;
            }
        }
        //copy back over
        for(int row = 0; row < Rows; ++row)
        {
            TokenGrid[row,col] = (row <= newRow)?null:newCol[row];
        }
        //add tweened
        for(int i = tweenedList.Count - 1; i >= 0; --i)
        {
            var t = tweenedList[i];
            TokenGrid[newRow,col] = t;
            t.OnLocationUpdate(newRow, col);
            TweenToken(t, t.Position, HolePosition(newRow+1,col+1));
            newRow--;
        }
    }

    public void FlipRow(int row)
    {
        var newRow = new TokenBase?[Columns];
        for(int col = 0; col < Columns; ++col)
        {
            TokenBase? t = TokenGrid[row,col];
            DisableTween(t);
            if(!IsInstanceValid(t)) TokenGrid[row,col] = null;
        }
        for(int col = 0; col < Columns; ++col)
        {
            TokenBase? t = TokenGrid[row,col];
            newRow[Columns-1-col] = t;
            if(t is not null)
            {
                t.OnLocationUpdate(row,Columns-col-1);
                t.GlobalPosition = HolePosition(row+1,Columns-col);
            }
        }
        for(int col = 0; col < Columns; ++col)
        {
            TokenGrid[row,col] = newRow[col];
        }
    }

    public void FlipVertical()
    {
        //i,j -> Rows-1-i,j
        var newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < Rows; ++row)
            for(int col = 0; col < Columns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                DisableTween(t);
                if(!IsInstanceValid(t)) TokenGrid[row,col] = null;
            }
        for(int row = 0; row < Rows; ++row)
            for(int col = 0; col < Columns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                newGrid[Rows-1-row,col] = t;
                if(t is not null)
                {
                    t.OnLocationUpdate(Rows-row-1,col);
                    t.GlobalPosition = HolePosition(Rows-row,col+1);
                }
            }
        TokenGrid = newGrid;
    }

    public void RotateLeft()
    {
        //i,j -> Columns-1-j,i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        BoardBase.Position = new Vector2(BoardBase.Position.Y, BoardBase.Position.X);
        BoardBase.Size = new Vector2(BoardBase.Size.Y, BoardBase.Size.X);
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                DisableTween(t);
                if(!IsInstanceValid(t)) TokenGrid[row,col] = null;
            }
        //swap
        (Rows,Columns) = (Columns,Rows);
        //go over grid
        var newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                newGrid[oldColumns-1-col,row] = t;
                if(t is not null)
                {
                    t.OnLocationUpdate(oldColumns-col-1,row);
                    t.GlobalPosition = HolePosition(oldColumns-col,row+1);
                }
            }
        TokenGrid = newGrid;
        CreateHoleMasks();
    }

    public void RotateRight()
    {
        //i,j -> j,Rows-1-i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        BoardBase.Position = new Vector2(BoardBase.Position.Y, BoardBase.Position.X);
        BoardBase.Size = new Vector2(BoardBase.Size.Y, BoardBase.Size.X);
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                DisableTween(t);
                if(!IsInstanceValid(t)) TokenGrid[row,col] = null;
            }
        //swap
        (Rows,Columns) = (Columns,Rows);
        //go over grid
        var newGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < oldRows; ++row)
            for(int col = 0; col < oldColumns; ++col)
            {
                TokenBase? t = TokenGrid[row,col];
                newGrid[col,oldRows-1-row] = t;
                if(t is not null)
                {
                    t.OnLocationUpdate(col,oldRows-row-1);
                    t.GlobalPosition = HolePosition(col+1,oldRows-row);
                }
            }
        TokenGrid = newGrid;
        CreateHoleMasks();
    }

    private static void DisableTween(TokenBase? t)
    {
        if(
            !t.IsInstanceValid() ||
            !t.TokenTween.IsTweenValid()
        ) return;

        t.TokenTween.StepToEnd();
        //we do another check incase that the tween finishing
        //has some additionally behavior that could
        //make it invalid
        if(!t.TokenTween.IsTweenValid())
            return;

        t.TokenTween.Kill();
        //early dispose this tween to avoid relying on the GC
        t.TokenTween.Dispose();
        t.TokenTween = null;
    }

    public void RemoveToken(int row, int col)
    {
        var t = TokenGrid[row,col];
        TokenGrid[row,col] = null;
        DisableTween(t);
        if(t.IsInstanceValid()) t.QueueFree();
    }

    private void TweenToken(TokenBase t, Vector2 from, Vector2 to)
    {
        if(!IsInstanceValid(t)) return;
        var distanceLeft = from.DistanceTo(to);

        if(!IsInstanceValid(t.TokenTween)) t.TokenTween = null;
        t.TokenTween?.Kill();
        t.TokenTween = t.CreateTween();
        //need reconnect
        if(t.TweenFinishedAction is not null)
        {
            t.ConnectTweenFinished();
        }

        t.TokenTween
            //tween
            .TweenProperty(
                //token
                t,
                //position
                Node2D.PropertyName.Position.ToString(),
                //to desired position
                to,
                //over the desired time
                distanceLeft/TokenDropSpeed
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In)
            //from the current position
            .From(from);
        t.TokenTween.Finished += RemoveFromTweenedTokensSet;
        t.TreeExited += RemoveFromTweenedTokensSet;
        _tweenedTokens.Add(t);
        
        void RemoveFromTweenedTokensSet()
        {
            if(!IsInsideTree()) return;
            _tweenedTokens.Remove(t);
            if(_tweenedTokens.Count <= 0)
            {
                DecideResult();
            }
        }
    }

    public virtual void DeserializeFrom(BoardData data)
    {
        if(data.Grid.Count != data.Rows)
            throw new ArgumentException($"Board data has row count of {data.Rows}, but its grid has {data.Grid.Count} rows");

        //cleanup
        _tweenedTokens.Clear();
        _ghostToken = null;
        for(int row = 0; row < Rows; ++row)
        {
            for(int col = 0; col < Columns; ++col)
            {
                TokenGrid[row,col]?.QueueFree();
            }
        }

        Rows = data.Rows;
        Columns = data.Columns;
        WinRequirement = data.WinRequirement;

        TokenGrid = new TokenBase?[Rows,Columns];
        for(int row = 0; row < Rows; ++row)
        {
            if(data.Grid[row].Count != data.Columns)
                throw new ArgumentException($"Board data has column count of {data.Columns}, but its {row}th row has {data.Grid[row].Count} elements");
            for(int col = 0; col < Columns; ++col)
            {
                var tdata = data.Grid[row][col];
                if(tdata is null) continue;
                var t = ResourceLoader.Load<PackedScene>(tdata.TokenScenePath).Instantiate<TokenBase>();
                TokenGrid[row,col] = t;
                t.Scale = TokenScale;
                AddChild(t);
                t.DeserializeFrom(this, tdata);
            }
        }
    }

    public virtual BoardData SerializeTo()
    {
        BoardData data = new()
        {
            Rows = Rows,
            Columns = Columns,
            WinRequirement = WinRequirement,
            Grid = new()
        };
        for(int row = 0; row < Rows; ++row)
        {
            data.Grid.Add(new Godot.Collections.Array<TokenData?>());
            for(int col = 0; col < Columns; ++col)
            {
                data.Grid[row].Add(TokenGrid[row,col]?.SerializeTo());
            }
        }
        return data;
    }
}
