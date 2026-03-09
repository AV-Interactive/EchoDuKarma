using Godot;
using System;
using EchoduKarma.Scripts.Data;

/// <summary>
/// Lead Developer: Camera script for a "Xenogears" RPG style.
/// Handles smooth 3D following of the player with a tilted overhead view.
/// </summary>
public partial class CameraFollow3D : Camera3D
{
    [Export] public bool UseCameraLimits = true;
    [Export] public NodePath TerrainPath;
    [Export] public float BorderMargin = 5f; // Marge avant le bord
    
    [Export] public Node3D Target;
    [Export] public float TargetFov = 40.0f;
    
    // Xenogears style typical offset (Top-down tilted)
    [Export] public Vector3 Offset = new Vector3(8, 10, 8); 
    
    [Export] public float SmoothSpeed = 5.0f;
    [Export] public Vector3 LookAtOffset = new Vector3(0, 0, 0);

    // New: Height adjustment for characters
    [Export] public float TargetHeightOffset = 1.0f;

    // Xenogears uses fixed rotation for the world camera
    [Export] public bool UseFixedRotation = true;
    [Export] public Vector3 FixedRotation = new Vector3(-35, 45, 0);

    [Export] public float RotationSpeed = 5.0f;
    
    private float _currentRotationDegrees = 45.0f;
    private float _targetRotationDegrees = 45.0f;
    private float _distanceToTarget;

    private Vector2 _mapMin, _mapMax;
    public Vector2 MapMin => _mapMin;
    public Vector2 MapMax => _mapMax;

    
    public override void _Ready()
    {
        // Auto détection du terrain pour le calcul de la distance
        if (!string.IsNullOrEmpty(TerrainPath.ToString()))
        {
            var terrain = GetNode<MeshInstance3D>(TerrainPath);
    
            // GetAabb() tient compte du transform global → plus fiable
            Aabb worldAabb = terrain.GlobalTransform * terrain.GetAabb();
    
            _mapMin = new Vector2(worldAabb.Position.X + BorderMargin, worldAabb.Position.Z + BorderMargin);
            _mapMax = new Vector2(worldAabb.End.X - BorderMargin, worldAabb.End.Z - BorderMargin);
    
            // Debug pour vérifier les valeurs
            GD.Print($"MapMin: {_mapMin} | MapMax: {_mapMax}");
            // Temporaire, juste pour debug
            GD.Print($"Player pos: {Target.GlobalPosition} | Cam pos: {GlobalPosition}");
        }
        
        // Try to find player if not assigned in Inspector
        if (Target == null && GameManager.Instance != null)
        {
            Target = GameManager.Instance.CurrentPlayer;
        }
        
        // Calculate initial distance based on Offset
        _distanceToTarget = new Vector2(Offset.X, Offset.Z).Length();
        _currentRotationDegrees = FixedRotation.Y;
        _targetRotationDegrees = _currentRotationDegrees;

        // Ensure initial position is set to avoid jumping
        if (Target != null && IsInstanceValid(Target))
        {
            UpdatePositionDirectly();
            ApplyRotation();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("rotate_left"))
        {
            _targetRotationDegrees += 90.0f;
        }
        else if (@event.IsActionPressed("rotate_right"))
        {
            _targetRotationDegrees -= 90.0f;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Fallback search for player if lost
        if (Target == null || !IsInstanceValid(Target))
        {
            if (GameManager.Instance != null)
                Target = GameManager.Instance.CurrentPlayer;
            return;
        }

        // 1. Handle rotation interpolation
        _currentRotationDegrees = Mathf.LerpAngle(
            Mathf.DegToRad(_currentRotationDegrees), 
            Mathf.DegToRad(_targetRotationDegrees), 
            (float)delta * RotationSpeed
        );
        _currentRotationDegrees = Mathf.RadToDeg(_currentRotationDegrees);

        // --- AJOUT ICI : Gestion du FOV (Zoom) ---
        // On lisse le changement de FOV vers ta valeur cible (ex: 40)
        Fov = Mathf.Lerp(Fov, TargetFov, (float)delta * SmoothSpeed);
        // -----------------------------------------

        // 2. Calculate the target position with the rotated offset
        Vector3 rotatedOffset = CalculateRotatedOffset();
        Vector3 targetPos = Target.GlobalPosition + rotatedOffset;

        // 3. Clamp le targetPos AVANT le Lerp
        if (UseCameraLimits && _mapMax != Vector2.Zero)
        {
            targetPos = new Vector3(
                Mathf.Clamp(targetPos.X, _mapMin.X, _mapMax.X),
                targetPos.Y,
                Mathf.Clamp(targetPos.Z, _mapMin.Y, _mapMax.Y)
            );
        }

        // 4. Smoothly interpolate to the target position (clampée)
        GlobalPosition = GlobalPosition.Lerp(targetPos, (float)delta * SmoothSpeed);

        // 5. Handle rotation
        ApplyRotation();
    }

    private Vector3 CalculateRotatedOffset()
    {
        float angleRad = Mathf.DegToRad(_currentRotationDegrees);
        return new Vector3(
            Mathf.Sin(angleRad) * _distanceToTarget,
            Offset.Y,
            Mathf.Cos(angleRad) * _distanceToTarget
        );
    }

    private void UpdatePositionDirectly()
    {
        GlobalPosition = Target.GlobalPosition + CalculateRotatedOffset();
    }

    private void ApplyRotation()
    {
        if (UseFixedRotation)
        {
            RotationDegrees = new Vector3(FixedRotation.X, _currentRotationDegrees, FixedRotation.Z);
        }
        else
        {
            LookAt(Target.GlobalPosition + LookAtOffset);
        }
    }
}
