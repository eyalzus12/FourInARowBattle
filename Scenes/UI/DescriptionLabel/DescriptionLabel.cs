using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace FourInARowBattle;

public partial class DescriptionLabel : Label
{
    public const string DEFAULT_TEXT = "Hover over a token to learn more about it";
    public const string REFILL_DESCRIPTION = "Press this button to get 1 more of each non-full token. You cannot use this button for two turns in a row.";

    private readonly static Dictionary<PackedScene, string> DescriptionCache = new();

    [Export]
    public GameTurnEnum ActiveOnTurn{get; set;}

    private string? _description = null;

    public override void _Ready()
    {
        //ensure label settings exist
        LabelSettings ??= new();

        UpdateDescription(null);
    }

    public void OnTokenHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        if(turn != ActiveOnTurn) return;

        _description = description;
        UpdateDescription(description);
    }

    public void OnTokenStopHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        if(
            turn != ActiveOnTurn ||
            description != _description
        ) return;

        _description = null;
        UpdateDescription(null);
    }

    /*
    There is no easy way to find the type of the root node of a PackedScene
    So we create an instance and cache the result
    */

    //this method returns null iff it is given null
    [return: NotNullIfNotNull(nameof(from))]
    public static string? DescriptionFromScene(PackedScene? from)
    {
        if(from is null) return null;
        else if(DescriptionCache.ContainsKey(from))
        {
            return DescriptionCache[from];
        }
        else
        {
            TokenBase token = Autoloads.ScenePool.GetScene<TokenBase>(from);
            string result = DescriptionCache[from] = token.TokenDescription;
            Autoloads.ScenePool.ReturnScene(token);
            return result;
        }
    }

    private void UpdateDescription(string? text)
    {
        Text = text ?? DEFAULT_TEXT;
        LabelSettings.FontColor = (text is null)?Colors.Gray:Colors.White;
    }
}
