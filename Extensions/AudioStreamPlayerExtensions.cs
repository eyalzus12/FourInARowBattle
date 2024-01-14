using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// Audio stream player extension functions
/// </summary>
public static class AudioStreamPlayerExtensions
{
    /// <summary>
    /// Play an audio stream in an audio player with the desired paramaters.
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="stream">The stream to play</param>
    /// <param name="bus"></param>
    /// <param name="maxPolyphony"></param>
    /// <param name="mixTarget"></param>
    /// <param name="pitchScale"></param>
    /// <param name="volumeDb"></param>
    public static void Play(
        this AudioStreamPlayer player, AudioStream stream,
        string bus = "Master",
        int maxPolyphony = 1,
        AudioStreamPlayer.MixTargetEnum mixTarget = AudioStreamPlayer.MixTargetEnum.Stereo,
        float pitchScale = 1,
        float volumeDb = 0
    )
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Stream = stream;

        player.Bus = bus;
        player.MaxPolyphony = maxPolyphony;
        player.MixTarget = mixTarget;
        player.PitchScale = pitchScale;
        player.VolumeDb = volumeDb;

        player.Play();
    }

    /// <summary>
    /// Play an audio stream in an audio player with the desired paramaters.
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="stream">The stream to play</param>
    /// <param name="position">The position to play in</param>
    /// <param name="areaMask"></param>
    /// <param name="attenuation"></param>
    /// <param name="bus"></param>
    /// <param name="maxDistance"></param>
    /// <param name="maxPolyphony"></param>
    /// <param name="panningStrength"></param>
    /// <param name="pitchScale"></param>
    /// <param name="volumeDb"></param>
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
        ArgumentNullException.ThrowIfNull(player);
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

    /// <summary>
    /// Play an audio stream in an audio player with the desired paramaters.
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="stream">The stream to play</param>
    /// <param name="position">The position to play in</param>
    /// <param name="areaMask"></param>
    /// <param name="attenuationFilterCutoffHz"></param>
    /// <param name="attenuationFilterDb"></param>
    /// <param name="attenuationModel"></param>
    /// <param name="bus"></param>
    /// <param name="dopplerTracking"></param>
    /// <param name="emissionAngleDegrees"></param>
    /// <param name="emissionAngleEnabled"></param>
    /// <param name="emissionAngleFilterAttenuationDb"></param>
    /// <param name="maxDb"></param>
    /// <param name="maxDistance"></param>
    /// <param name="maxPolyphony"></param>
    /// <param name="panningStrength"></param>
    /// <param name="pitchScale"></param>
    /// <param name="unitSize"></param>
    /// <param name="volumeDb"></param>
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
        ArgumentNullException.ThrowIfNull(player);
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