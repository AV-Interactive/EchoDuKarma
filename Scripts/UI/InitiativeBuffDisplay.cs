using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>Icône de buff affichée sur une ligne du panneau d'initiative.</summary>
public sealed class InitiativeBuffDisplay
{
    public Texture2D Icon { get; init; }
    public int TurnsLeft { get; init; }
    public string Tooltip { get; init; } = "";
}
