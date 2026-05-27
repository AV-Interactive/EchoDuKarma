using Godot;
using System.Collections.Generic;

namespace EchoduKarma.Scripts.UI;

/// <summary>Ligne affichée dans le panneau d'initiative (ordre d'exécution du round).</summary>
public sealed class InitiativeDisplayEntry
{
    public string DisplayName { get; init; } = "";
    public Texture2D Portrait { get; init; }
    public int Initiative { get; init; }
    public string ActionLabel { get; init; } = "";
    public bool IsPlayer { get; init; }
    public bool IsPending { get; init; }
    public bool IsActive { get; init; }
    public bool IsCompleted { get; init; }
    public IReadOnlyList<InitiativeBuffDisplay> Buffs { get; init; } = System.Array.Empty<InitiativeBuffDisplay>();
}
