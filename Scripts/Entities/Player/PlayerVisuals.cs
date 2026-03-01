using Godot;
using System;

public partial class PlayerVisuals : Sprite3D
{
    [Export] AnimationPlayer _animPlayer;
    [Export] public bool usePixelPerfectAlignement = true;
    Camera3D _cam;

    // Variable mémoire pour stocker la dernière direction
    string _lastDirection = "DOWN";

    public override void _Ready()
    {
        _cam = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        if (usePixelPerfectAlignement)
        {
            Vector3 globalPosition = GlobalPosition;
            globalPosition.X = Mathf.Round(globalPosition.X * 16) / 16;
            globalPosition.Y = Mathf.Round(globalPosition.Y * 16) / 16;
        }
    }

    public void UpdateFrame(Vector3 velocity)
    {
        if (_animPlayer == null) return;

        // --- GESTION DE L'ARRÊT ---
        if (velocity.Length() < 0.1f)
        {
            // On joue l'IDLE correspondant à la dernière direction
            // Exemple : "IDLE_LEFT", "IDLE_UP", etc.
            string idleAnim = "IDLE_" + _lastDirection;
            
            // Si tu n'as pas encore d'anims IDLE par direction, 
            // on peut juste stopper l'anim de marche sur la frame actuelle :
            if (_animPlayer.HasAnimation(idleAnim))
                _animPlayer.Play(idleAnim);
            else
                _animPlayer.Stop(); 
                
            return;
        }

        // --- CALCUL DE LA DIRECTION (Inchangé) ---
        Vector3 camForward = -_cam.GlobalTransform.Basis.Z;
        camForward.Y = 0;
        camForward = camForward.Normalized();

        Vector3 camRight = _cam.GlobalTransform.Basis.X;
        camRight.Y = 0;
        camRight = camRight.Normalized();

        float forwardDot = velocity.Normalized().Dot(camForward);
        float rightDot = velocity.Normalized().Dot(camRight);

        string currentDir = "";

        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
        {
            currentDir = (forwardDot > 0) ? "UP" : "DOWN";
        }
        else
        {
            currentDir = (rightDot > 0) ? "RIGHT" : "LEFT";
        }

        // --- MISE À JOUR ET MÉMOIRE ---
        _lastDirection = currentDir; // On enregistre la direction

        if (_animPlayer.CurrentAnimation != currentDir)
        {
            _animPlayer.Play(currentDir);
        }
    }
}
