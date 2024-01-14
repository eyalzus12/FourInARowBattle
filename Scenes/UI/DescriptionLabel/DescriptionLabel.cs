using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace FourInARowBattle;

/// <summary>
/// This is a label that show the description of a token, or the refill button.
/// Because we set the description in the editor, it is an instance value
/// and we cannot easily grab that from the scene.
/// So the description label loads instances of that token and caches the description.
/// </summary>
public partial class DescriptionLabel : Label
{
    public const string DEFAULT_TEXT = "Hover over a token to learn more about it";
    public const string REFILL_DESCRIPTION = "Press this button to get 1 more of each non-full token. You cannot use this button for two turns in a row.";

    private readonly static Dictionary<PackedScene, string> DescriptionCache = new();

    /// <summary>
    /// Which player turn the label corresponds to
    /// </summary>
    [Export]
    public GameTurnEnum ActiveOnTurn{get; private set;}

    private string? _description = null;

    public override void _Ready()
    {
        //ensure label settings exist
        LabelSettings ??= new();

        UpdateDescription(null);
    }

    /// <summary>
    /// Token hovered
    /// </summary>
    /// <param name="turn">The player turn of the token</param>
    /// <param name="description">The token description</param>
    public void OnTokenHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        if(turn != ActiveOnTurn) return;

        _description = description;
        UpdateDescription(description);
    }

    /// <summary>
    /// Token stop being hovered
    /// </summary>
    /// <param name="turn">The player turn of the token</param>
    /// <param name="description">The token description</param>
    public void OnTokenStopHover(GameTurnEnum turn, string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        if(turn != ActiveOnTurn || description != _description) return;

        _description = null;
        UpdateDescription(null);
    }


    /// <summary>
    /// Grab the token description from a token scene. Only returns null if the given scene is null.
    /// </summary>
    /// <param name="from">The scene</param>
    /// <returns>The description</returns>
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

    /// <summary>
    /// Update the label description
    /// </summary>
    /// <param name="text">The new text</param>
    private void UpdateDescription(string? text)
    {
        Text = text ?? DEFAULT_TEXT;
        LabelSettings.FontColor = (text is null)?Colors.Gray:Colors.White;
    }
}
