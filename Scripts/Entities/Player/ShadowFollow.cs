using Godot;
using System;

public partial class ShadowFollow : Sprite3D
{
    [Export] public Node3D PlayerBody; // Ton CharacterBody3D

    public override void _Process(double delta)
    {
        if (PlayerBody == null) return;

        // On suit la position X et Z du corps du joueur
        // Mais on garde notre Y fixe (proche du sol)
        Vector3 newPos = GlobalPosition;
        newPos.Y = PlayerBody.GlobalPosition.Y - .355f;
        newPos.X = PlayerBody.GlobalPosition.X;
        newPos.Z = PlayerBody.GlobalPosition.Z;
        
        // Optionnel : On peut ajuster l'opacité selon la hauteur (si le perso saute)
        // float distToGround = PlayerBody.GlobalPosition.Y - hauteurDuSol;
        
        GlobalPosition = newPos;
    }
}
