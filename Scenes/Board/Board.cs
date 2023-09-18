using Godot;
using System.Collections.Generic;
using System;
using System.Diagnostics.Metrics;

public partial class Board : Node2D
{
    public static readonly StringName TOKEN_TWEEN_META_NAME = "DropTween";

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

    private CanvasGroup _canvasGroup = null!;
    private ColorRect _colorRect = null!;
    private Node2D? _maskGroup = null;

    public Vector2 BoardPosition => _colorRect.GlobalPosition + new Vector2(LeftMargin,TopMargin);
    public Vector2 BoardSize => _colorRect.Size - new Vector2(RightMargin,BottomMargin);

    private Vector2 HoleJump => BoardSize / new Vector2(Columns+1, Rows+1);
    private Vector2 CenterOffset => SlotRadius * Vector2.One;
    public Vector2 HolePosition(int row, int col) => BoardPosition + HoleJump*new Vector2(col,row) - CenterOffset;

    private readonly record struct GhostTokenRenderData(Texture2D TokenTexture, Color TokenColor, int Column){}
    private GhostTokenRenderData? _ghostToken;

    public override void _Ready()
    {
        _canvasGroup = GetNode<CanvasGroup>("CanvasGroup");
        _colorRect = _canvasGroup.GetNode<ColorRect>("ColorRect");

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
        _colorRect.AddSibling(_maskGroup);
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
        Dictionary<GameResultEnum, int> resultCounts = new();
        bool haveFree = false;
        for(int row = 0; row < Rows; ++row)
        {
            for(int col = 0; col < Columns; ++col)
            {
                if(TokenGrid[row,col] is not null)
                    CheckSpotWin(row, col, resultCounts);
                else
                    haveFree = true;
            }
        }
        if(!haveFree) return GameResultEnum.Draw;

        List<GameResultEnum> mostCommonResults = new();
        int maxCount = 1;
        foreach(var (result, count) in resultCounts)
        {
            if(count == maxCount) mostCommonResults.Add(result);
            else if(count > maxCount)
            {
                mostCommonResults = new(){result};
                maxCount = count;
            }
        }

        return mostCommonResults.Count switch
        {
            //no wins
            0 => GameResultEnum.None,
            //exactly one win
            1 => mostCommonResults[0],
            //multiple wins at the same time
            >= 2 => GameResultEnum.Draw,
            //invalid/negative value
            _ => GameResultEnum.None
        };
    }

    private void CheckSpotWin(int row, int col, Dictionary<GameResultEnum, int> resultCounts)
    {
        TokenBase? token = TokenGrid[row,col];
        if(!IsInstanceValid(token)) TokenGrid[row,col] = token = null;
        if(token is null || token.Result == GameResultEnum.None) return;
        resultCounts.TryAdd(token.Result, 0);
        //check up
        if(row >= WinRequirement-1)
        {
            bool upFail = false;
            for(int rowOffset = 1; rowOffset < WinRequirement; rowOffset++)
                if(!token.SameAs(TokenGrid[row-rowOffset,col]))
                {
                    upFail = true;
                    break;
                }
            if(!upFail) resultCounts[token.Result]++;
        }
        //check left
        if(col >= WinRequirement-1)
        {
            bool leftFail = false;
            for(int colOffset = 1; colOffset < WinRequirement; colOffset++)
                if(!token.SameAs(TokenGrid[row,col-colOffset]))
                {
                    leftFail = true;
                    break;
                }
            if(!leftFail) resultCounts[token.Result]++;
        }
        //check up left
        if(row >= WinRequirement-1 && col >= WinRequirement-1)
        {
            bool upLeftFail = false;
            for(int offset = 1; offset < WinRequirement; offset++)
                if(!token.SameAs(TokenGrid[row-offset,col-offset]))
                {
                    upLeftFail = true;
                    break;
                }
            if(!upLeftFail) resultCounts[token.Result]++;
        }
        //check up right
        if(row >= WinRequirement-1 && col <= Columns-WinRequirement)
        {
            bool upRightFail = false;
            for(int offset = 1; offset < WinRequirement; offset++)
                if(!token.SameAs(TokenGrid[row-offset,col+offset]))
                {
                    upRightFail = true;
                    break;
                }
            if(!upRightFail) resultCounts[token.Result]++;
        }
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
        List<TokenBase?> tokens = new();
        for(int row = Rows-1; row >= 0; row--)
        {
            TokenBase? t = TokenGrid[row,col];
            if(!IsInstanceValid(t)) TokenGrid[row,col] = t = null;
            if(t is not null)
            {
                //disable tween early so that t.Position is accurate
                DisableTween(t);
                tokens.Add(t);
            }
        }
        int tokenIdx = Rows-1;
        foreach(var t in tokens)
        {
            //because IsInstanceValid does not account for same-frame QueueFree,
            //we also check that no QueueFree was called
            if(t is not null && IsInstanceValid(t) && !t.IsQueuedForDeletion())
            {
                TokenGrid[tokenIdx,col] = t;
                TweenToken(t, t.Position, HolePosition(tokenIdx+1,col+1));
                tokenIdx--;
            }
        }
        for(; tokenIdx >= 0; tokenIdx--)
        {
            TokenGrid[tokenIdx,col] = null;
        }
        _colGravityLock.Remove(col);
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
                    t.GlobalPosition = HolePosition(Rows-row,col+1);
            }
        TokenGrid = newGrid;
    }

    public void RotateLeft()
    {
        //i,j -> Columns-1-j,i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        _colorRect.Position = new Vector2(_colorRect.Position.Y, _colorRect.Position.X);
        _colorRect.Size = new Vector2(_colorRect.Size.Y, _colorRect.Size.X);
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
                    t.GlobalPosition = HolePosition(oldColumns-col,row+1);
            }
        TokenGrid = newGrid;
        CreateHoleMasks();
    }

    public void RotateRight()
    {
        //i,j -> j,Rows-1-i
        int oldRows = Rows, oldColumns = Columns; 
        //rotate
        _colorRect.Position = new Vector2(_colorRect.Position.Y, _colorRect.Position.X);
        _colorRect.Size = new Vector2(_colorRect.Size.Y, _colorRect.Size.X);
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
                    t.GlobalPosition = HolePosition(col+1,oldRows-row);
            }
        TokenGrid = newGrid;
        CreateHoleMasks();
    }

    private static void DisableTween(TokenBase? t)
    {
        if(t is not null && IsInstanceValid(t) && t.HasMeta(TOKEN_TWEEN_META_NAME))
        {
            var oldTween = (Tween)t.GetMeta(TOKEN_TWEEN_META_NAME);
            if(IsInstanceValid(oldTween) && oldTween.IsValid())
            {
                oldTween.CustomStep(double.PositiveInfinity);
                //we do another check incase that the tween finishing
                //has some additionally behavior that could
                //make it invalid
                if(IsInstanceValid(oldTween) && oldTween.IsValid())
                {
                    oldTween.Kill();
                    //early dispose this tween to avoid relying on the GC
                    oldTween.Dispose();
                }
            }
        }
    }

    public void RemoveToken(int row, int col)
    {
        if(row >= Rows) GD.Print(row);
        if(row < 0) GD.Print(row);
        var t = TokenGrid[row,col];
        TokenGrid[row,col] = null;
        DisableTween(t);
        if(t is not null && IsInstanceValid(t)) t.QueueFree();
    }

    private void TweenToken(TokenBase t, Vector2 from, Vector2 to)
    {
        DisableTween(t);
        if(!IsInstanceValid(t)) return;

        var distanceLeft = from.DistanceTo(to);
        var tween = t.CreateTween();
        tween
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
        t.SetMeta(TOKEN_TWEEN_META_NAME, tween);
    }
}
