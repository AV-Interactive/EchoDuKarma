using EchoduKarma.Scripts.Entities.Player;

/// <summary>Slot de tour dans un round (ordre initiative), ou action ennemie planifiée.</summary>
public sealed class WaveActionEntry
{
    public enum ActionKind
    {
        /// <summary>Tour joueur — choix au moment de jouer.</summary>
        PlayerWaiting,
        PlayerPhysical,
        PlayerMagic,
        PlayerHeal,
        PlayerBuff,
        PlayerDefend,
        PlayerFlee,
        EnemyAttack,
        EnemyMagic,
        EnemyHeal,
        EnemyBuff,
        EnemyDefend,
    }

    public IBattler Battler { get; init; }
    public ActionKind Kind { get; init; }
    public int Initiative { get; init; }
    public Skill Skill { get; init; }
    public int TargetIndex { get; init; } = -1;
    public Enemy Enemy { get; init; }
}
