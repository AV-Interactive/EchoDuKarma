using Godot;
using System;
using EchoduKarma.Scripts.Data;

/// <summary>
/// Lead Developer: Camera script for a "Xenogears" RPG style.
/// Handles smooth 3D following of the player with a tilted overhead view.
/// </summary>
public partial class CameraFollow3D : Camera3D
{
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

    public override void _Ready()
    {
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
    
        // 3. Smoothly interpolate to the target position
        GlobalPosition = GlobalPosition.Lerp(targetPos, (float)delta * SmoothSpeed);
    
        // 4. Handle rotation
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
