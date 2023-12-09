using Godot;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace FourInARowBattle;

public static class Extensions
{
    //shorthands for meta stuff
    public static Variant? GetMetaOrNull(this GodotObject o, StringName s) =>
        o.HasMeta(s) ? (Variant?)o.GetMeta(s) : null;
    public static T GetMeta<[MustBeVariant] T>(this GodotObject o, StringName s) =>
        o.GetMeta(s).As<T>();
    //make a tween finish.
    public static void StepToEnd(this Tween t) =>
        t.CustomStep(float.PositiveInfinity);
    //common conversions between enums and stuff
    //the 9999 is a value assumed to be invalid
    public static Color GameTurnToColor(this GameTurnEnum g) => g switch
    {
        GameTurnEnum.Player1 => Colors.Red,
        GameTurnEnum.Player2 => Colors.Blue,
        _ => Colors.White
    };
    public static GameTurnEnum GameResultToGameTurn(this GameResultEnum g) => g switch
    {
        GameResultEnum.Player1Win => GameTurnEnum.Player1,
        GameResultEnum.Player2Win => GameTurnEnum.Player2,
        _ => (GameTurnEnum)9999
    };

    /*
        the default IsInstanceValid does not tell the compiler
        that the paramater is not null if it returns true
        so this is a wrapper that does that, and also has extra checks
    */
    public static bool IsInstanceValid([NotNullWhen(true)] this GodotObject? o) =>
        o is not null && GodotObject.IsInstanceValid(o) && !o.IsQueuedForDeletion();
    public static bool IsTweenValid([NotNullWhen(true)] this Tween? t) =>
        t.IsInstanceValid() && t.IsValid();
    //Linq-like conversion from IEnumerable to godot array
    public static Godot.Collections.Array<T> ToGodotArray<[MustBeVariant] T>(this IEnumerable<T> e) => new(e);
    //Convert a Rect2 to a list of positions, to be used for functions requiring polygons
    public static Vector2[] ToPolygon(this Rect2 r) => new[]{r.Position, r.Position + r.Size*Vector2.Right, r.Position + r.Size, r.Position + r.Size*Vector2.Down};
    //Helper to move controls around
    public static void CenterOn(this Control c, Vector2 at) => c.GlobalPosition = at - c.GetGlobalRect().Size/2;
    
    public static Vector2I GetVisibleSize(this Window w) => (Vector2I)w.GetVisibleRect().Size;
    public static Vector2I GetSizeOfDecorations(this Window w) => w.GetSizeWithDecorations() - w.Size;

    public static bool IsJustPressed(this InputEvent @event) => @event.IsPressed() && !@event.IsEcho();

    public static void SetDeferredDisabled(this CollisionShape2D col, bool disabled) => col.SetDeferred(CollisionShape2D.PropertyName.Disabled, disabled);

    public static bool ContainsNotNull<T>(this HashSet<T> set, T? t) => t is not null && set.Contains(t);

    public static void Play(
        this AudioStreamPlayer player, AudioStream stream,
        string bus = "Master",
        int maxPolyphony = 1,
        AudioStreamPlayer.MixTargetEnum mixTarget = AudioStreamPlayer.MixTargetEnum.Stereo,
        float pitchScale = 1,
        float volumeDb = 0
    )
    {
        player.Stream = stream;

        player.Bus = bus;
        player.MaxPolyphony = maxPolyphony;
        player.MixTarget = mixTarget;
        player.PitchScale = pitchScale;
        player.VolumeDb = volumeDb;

        player.Play();
    }

    public static void Play(
        this AudioStreamPlayer2D player, AudioStream stream, Vector2 position,
        uint areaMask = 1,
        float attenuation = 1,
        string bus = "Master",
        float maxDistance = 2000,
        int maxPolyphony = 1,
        float panningStrength = 1,
        float pitchScale = 1,
        float volumeDb = 0
    )
    {
        player.Stream = stream;
        player.GlobalPosition = position;

        player.AreaMask = areaMask;
        player.Attenuation = attenuation;
        player.Bus = bus;
        player.MaxDistance = maxDistance;
        player.MaxPolyphony = maxPolyphony;
        player.PanningStrength = panningStrength;
        player.PitchScale = pitchScale;
        player.VolumeDb = volumeDb;

        player.Play();
    }

    public static void Play(
        this AudioStreamPlayer3D player, AudioStream stream, Vector3 position,
        uint areaMask = 1,
        float attenuationFilterCutoffHz = 5000,
        float attenuationFilterDb = -24,
        AudioStreamPlayer3D.AttenuationModelEnum attenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance,
        string bus = "Master",
        AudioStreamPlayer3D.DopplerTrackingEnum dopplerTracking = AudioStreamPlayer3D.DopplerTrackingEnum.Disabled,
        float emissionAngleDegrees = 45,
        bool emissionAngleEnabled = false,
        float emissionAngleFilterAttenuationDb = -12,
        float maxDb = 3,
        float maxDistance = 0,
        int maxPolyphony = 1,
        float panningStrength = 1,
        float pitchScale = 1,
        float unitSize = 10,
        float volumeDb = 1
    )
    {
        player.Stream = stream;
        player.GlobalPosition = position;

        player.AreaMask = areaMask;
        player.AttenuationFilterCutoffHz = attenuationFilterCutoffHz;
        player.AttenuationFilterDb = attenuationFilterDb;
        player.AttenuationModel = attenuationModel;
        player.Bus = bus;
        player.DopplerTracking = dopplerTracking;
        player.EmissionAngleDegrees = emissionAngleDegrees;
        player.EmissionAngleEnabled = emissionAngleEnabled;
        player.EmissionAngleFilterAttenuationDb = emissionAngleFilterAttenuationDb;
        player.MaxDb = maxDb;
        player.MaxDistance = maxDistance;
        player.MaxPolyphony = maxPolyphony;
        player.PanningStrength = panningStrength;
        player.PitchScale = pitchScale;
        player.UnitSize = unitSize;
        player.VolumeDb = volumeDb;


        player.Play();
    }
}
