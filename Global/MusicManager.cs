using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Musique de fond globale (menu, zones, combat) avec boucle et fondus.
/// </summary>
public partial class MusicManager : Node
{
    public const string MenuTrackPath = "res://Assets/Musics/L_echo_du_Karma.mp3";
    public const string IntroductionTrackPath = "res://Assets/Musics/L_Etoffe_du_matin.mp3";
    public const string BattleTrackPath = "res://Assets/Musics/L_heure_du_glas.mp3";

    public static MusicManager Instance { get; private set; }

    static readonly Dictionary<string, string> ZoneTrackPaths = new()
    {
        ["Introduction"] = IntroductionTrackPath,
    };

    AudioStreamPlayer _player;
    string _currentTrackPath = "";
    Tween _volumeTween;

    [Export] public float DefaultVolumeDb = -4f;
    [Export] public float FadeInSeconds = 1.2f;
    [Export] public float FadeOutSeconds = 0.55f;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        _player = new AudioStreamPlayer
        {
            Name = "MusicPlayer",
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_player);
        _player.Finished += OnTrackFinished;

        GD.Print("[MusicManager] Ready.");
    }

    public override void _ExitTree()
    {
        if (_player != null)
            _player.Finished -= OnTrackFinished;

        if (Instance == this)
            Instance = null;

        base._ExitTree();
    }

    public void PlayMenu() => PlayTrack(MenuTrackPath);

    public void PlayBattle() => PlayTrack(BattleTrackPath);

    public void PlayZone(string zoneName)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            PlayTrack(IntroductionTrackPath);
            return;
        }

        string zone = zoneName.Trim();
        if (ZoneTrackPaths.TryGetValue(zone, out string path))
            PlayTrack(path);
        else
        {
            GD.Print($"[MusicManager] Zone '{zone}' sans musique dédiée — défaut Introduction.");
            PlayTrack(IntroductionTrackPath);
        }
    }

    public void PlayTrack(string path, bool forceRestart = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!forceRestart && _currentTrackPath == path && _player.Playing)
            return;

        AudioStream stream = LoadLoopedStream(path);
        if (stream == null)
        {
            GD.PrintErr($"[MusicManager] Impossible de charger : {path}");
            return;
        }

        _volumeTween?.Kill();
        _currentTrackPath = path;
        _player.Stream = stream;
        _player.VolumeDb = -40f;
        _player.Play();

        _volumeTween = CreateTween();
        _volumeTween.TweenProperty(_player, "volume_db", DefaultVolumeDb, FadeInSeconds)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        GD.Print($"[MusicManager] Lecture : {path}");
    }

    public void FadeOutAndStop(float durationSeconds = -1f)
    {
        if (durationSeconds < 0f)
            durationSeconds = FadeOutSeconds;

        if (!_player.Playing && string.IsNullOrEmpty(_currentTrackPath))
            return;

        _volumeTween?.Kill();
        _volumeTween = CreateTween();
        _volumeTween.TweenProperty(_player, "volume_db", -40f, durationSeconds)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        _volumeTween.TweenCallback(Callable.From(StopMusic));
    }

    void StopMusic()
    {
        _player.Stop();
        _currentTrackPath = "";
    }

    void OnTrackFinished()
    {
        if (_player.Stream == null)
            return;

        _player.Play();
    }

    public static AudioStream LoadLoopedStream(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
            return null;

        var loaded = ResourceLoader.Load<AudioStream>(path);
        if (loaded == null)
            return null;

        AudioStream stream = (AudioStream)loaded.Duplicate();

        if (stream is AudioStreamMP3 mp3)
            mp3.Loop = true;
        else if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = true;

        return stream;
    }
}
