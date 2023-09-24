using Godot;
using System;
using System.Collections.Generic;

public partial class DescriptionLabel : Label
{
    public const string DEFAULT_TEXT = "Hover over a token to learn more about it";

    private readonly static Dictionary<PackedScene, string> DescriptionCache = new();

    [Export]
    public GameTurnEnum ActiveOnTurn{get; set;}

    private EventBus _eventBus = null!;

    private PackedScene? _scene = null;

    public override void _Ready()
    {
        //ensure label settings exist
        LabelSettings ??= new();

        UpdateDescription(null);

        _eventBus = GetTree().Root.GetNode<EventBus>(nameof(EventBus));

        _eventBus.TokenButtonHovered += OnTokenHover;
        _eventBus.TokenButtonStoppedHover += OnTokenStopHover;
    }

    private void OnTokenHover(GameTurnEnum turn, PackedScene scene)
    {
        if(turn != ActiveOnTurn) return;

        _scene = scene;
        UpdateDescription(DescriptionFromScene(scene));
    }

    private void OnTokenStopHover(GameTurnEnum turn, PackedScene scene)
    {
        if(
            turn != ActiveOnTurn ||
            scene != _scene
        ) return;

        _scene = null;
        UpdateDescription(null);
    }

    private static string? DescriptionFromScene(PackedScene? from)
    {
        if(from is null) return null;
        else if(DescriptionCache.ContainsKey(from))
        {
            return DescriptionCache[from];
        }
        else
        {
            var token = from.Instantiate<TokenBase>();
            var result = DescriptionCache[from] = token.TokenDescription;
            token.QueueFree();
            return result;
        }
    }

    private void UpdateDescription(string? text)
    {
        Text = text ?? DEFAULT_TEXT;
        LabelSettings.FontColor = (text is null)?Colors.Gray:Colors.White;
    }
}
