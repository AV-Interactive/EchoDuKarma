using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class CameraDirector : Node3D
{
    public enum CameraShot
    {
        Neutral,
        PlayerAttack,
        PlayerMagic,
        EnemyAttack,
    }

    const float FADE_DURATION = 0.2f;
    const float FocusHeightOffset = 0.45f;
    const int FrameIterations = 6;
    const float FrameTolerancePx = 2f;

    static readonly CameraShot[] FramableShots = { CameraShot.PlayerAttack, CameraShot.PlayerMagic };

    [Export] ColorRect _fadeOverlay;

    Dictionary<CameraShot, Camera3D> _shots;
    Dictionary<CameraShot, Transform3D> _shotTemplates;
    Dictionary<CameraShot, Vector3> _focusOffsets;

    Camera3D _activeCamera;

    public override void _Ready()
    {
        _shots = new Dictionary<CameraShot, Camera3D>
        {
            { CameraShot.Neutral,       GetNode<Camera3D>("ShotNeutral") },
            { CameraShot.PlayerAttack,  GetNode<Camera3D>("ShotPlayerAttack") },
            { CameraShot.PlayerMagic,   GetNode<Camera3D>("ShotPlayerMagic") },
            { CameraShot.EnemyAttack,   GetNode<Camera3D>("ShotEnemyAttack") }
        };

        _shotTemplates = new Dictionary<CameraShot, Transform3D>();
        _focusOffsets = new Dictionary<CameraShot, Vector3>();
        foreach (var (shot, cam) in _shots)
            _shotTemplates[shot] = cam.Transform;

        foreach (var cam in _shots.Values)
            cam.Current = false;

        CutTo(CameraShot.Neutral, instant: true);
    }

    /// <summary>
    /// Capture l'écart caméra / centre du groupe d'ennemis pour conserver
    /// l'angle et la hauteur du plan défini dans l'éditeur.
    /// </summary>
    public void RegisterBattleFocus(Vector3 enemyGroupCenter)
    {
        Vector3 focusPoint = enemyGroupCenter + new Vector3(0, FocusHeightOffset, 0);

        foreach (var shot in FramableShots)
        {
            var cam = _shots[shot];
            cam.Transform = _shotTemplates[shot];
            _focusOffsets[shot] = cam.GlobalPosition - focusPoint;
        }
    }

    public Task CutTo(CameraShot shot, bool instant = false) =>
        CutTo(shot, null, instant);

    public async Task CutTo(CameraShot shot, Node3D focusTarget, bool instant = false)
    {
        if (!_shots.TryGetValue(shot, out var targetCam))
            return;

        if (focusTarget != null && IsFramableShot(shot))
            FrameTargetOnScreen(targetCam, focusTarget, shot);
        else if (_shotTemplates.TryGetValue(shot, out var template))
            targetCam.Transform = template;

        bool alreadyActive = targetCam == _activeCamera;

        if (instant)
        {
            if (!alreadyActive && _activeCamera != null)
                _activeCamera.Current = false;

            targetCam.Current = true;
            _activeCamera = targetCam;
            return;
        }

        if (!alreadyActive)
        {
            await FadeOut();

            if (_activeCamera != null)
                _activeCamera.Current = false;

            targetCam.Current = true;
            _activeCamera = targetCam;

            await FadeIn();
        }
    }

    static bool IsFramableShot(CameraShot shot) =>
        shot is CameraShot.PlayerAttack or CameraShot.PlayerMagic;

    void FrameTargetOnScreen(Camera3D camera, Node3D target, CameraShot shot)
    {
        camera.Transform = _shotTemplates[shot];

        Vector3 focusPoint = target.GlobalPosition + new Vector3(0, FocusHeightOffset, 0);

        if (_focusOffsets.TryGetValue(shot, out Vector3 offset))
            camera.GlobalPosition = focusPoint + offset;

        CenterHorizontallyOnScreen(camera, focusPoint);
    }

    /// <summary>
    /// Recadre uniquement sur l'axe horizontal écran — pas de déplacement vertical
    /// pour ne pas remonter la caméra vers le ciel.
    /// </summary>
    static void CenterHorizontallyOnScreen(Camera3D camera, Vector3 focusPoint)
    {
        Vector2 screenCenter = camera.GetViewport().GetVisibleRect().Size / 2f;

        for (int i = 0; i < FrameIterations; i++)
        {
            Vector2 screenPos = camera.UnprojectPosition(focusPoint);
            float deltaX = screenCenter.X - screenPos.X;

            if (Mathf.Abs(deltaX) < FrameTolerancePx)
                break;

            Transform3D camTransform = camera.GlobalTransform;
            float distance = camTransform.Origin.DistanceTo(focusPoint);
            float viewportHeight = camera.GetViewport().GetVisibleRect().Size.Y;
            float pixelToWorld = distance * 2f * Mathf.Tan(Mathf.DegToRad(camera.Fov * 0.5f)) / viewportHeight;

            // Pan horizontal au sol uniquement — évite de remonter vers le ciel.
            Vector3 panAxis = camTransform.Basis.X;
            panAxis.Y = 0f;
            if (panAxis.LengthSquared() < 0.001f)
                break;

            panAxis = panAxis.Normalized();
            camera.GlobalPosition -= panAxis * deltaX * pixelToWorld;
        }
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
