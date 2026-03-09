using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class CameraDirector : Node3D
{
    public enum CameraShot
    {
        Neutral,
        PlayerAttack,
        EnemyAttack,
    }
    
    const float FADE_DURATION = 0.2f;

    [Export] ColorRect _fadeOverlay;
    
    Dictionary<CameraShot, Camera3D> _shots;
    
    Camera3D _activeCamera;
    
    public override void _Ready()
    {
        _shots = new Dictionary<CameraShot, Camera3D>
        {
            { CameraShot.Neutral,       GetNode<Camera3D>("ShotNeutral") },
            { CameraShot.PlayerAttack,  GetNode<Camera3D>("ShotPlayerAttack") },
            { CameraShot.EnemyAttack,   GetNode<Camera3D>("ShotEnemyAttack") }
        };

        foreach (var cam in _shots.Values)
        {
            cam.Current = false;
        }

        CutTo(CameraShot.Neutral, instant: true);
    }

    public async Task CutTo(CameraShot shot, bool instant = false)
    {
        if(!_shots.TryGetValue(shot, out var targetCam)) return;
        if (targetCam == _activeCamera) return;

        if (instant)
        {
            if (_activeCamera != null)
                _activeCamera.Current = false;

            targetCam.Current = true;
            _activeCamera = targetCam;
            return;
        }

        await FadeOut();

        if (_activeCamera != null) _activeCamera.Current = false;
        targetCam.Current = true;
        _activeCamera = targetCam;

        await FadeIn();
    }

    async Task FadeOut()
    {
        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "modulate:a", 1, FADE_DURATION);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
    
    async Task FadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "modulate:a", 0, FADE_DURATION);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
