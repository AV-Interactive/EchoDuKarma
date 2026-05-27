using System.Collections.Generic;

/// <summary>Types de buffs temporaires suivis en combat.</summary>
public enum CombatBuffKind
{
    Force,
    Heavy,
    Sleep,
}

/// <summary>État affichable d'un buff actif sur un combattant.</summary>
public readonly struct CombatBuffSnapshot
{
    public CombatBuffKind Kind { get; init; }
    public int Amount { get; init; }
    public int TurnsLeft { get; init; }
    public string SourceName { get; init; }
}

/// <summary>Suivi des buffs temporaires en combat (durée en rounds).</summary>
public sealed class CombatBuffTracker
{
    sealed class ForceBuff
    {
        public int Amount;
        public int TurnsLeft;
        public string SourceName;
    }

    readonly Dictionary<IBattler, ForceBuff> _forceBuffs = new();

    public int GetForceBonus(IBattler battler) =>
        battler != null && _forceBuffs.TryGetValue(battler, out ForceBuff buff)
            ? buff.Amount
            : 0;

    public void ApplyForceBuff(IBattler battler, int amount, int turns, string sourceName)
    {
        if (battler == null || amount <= 0 || turns <= 0)
            return;

        _forceBuffs[battler] = new ForceBuff
        {
            Amount = amount,
            TurnsLeft = turns,
            SourceName = sourceName ?? "Renforcement",
        };
    }

    public IReadOnlyList<CombatBuffSnapshot> GetSnapshots(IBattler battler)
    {
        if (battler == null || !_forceBuffs.TryGetValue(battler, out ForceBuff buff))
            return System.Array.Empty<CombatBuffSnapshot>();

        return new[]
        {
            new CombatBuffSnapshot
            {
                Kind = CombatBuffKind.Force,
                Amount = buff.Amount,
                TurnsLeft = buff.TurnsLeft,
                SourceName = buff.SourceName,
            },
        };
    }

    /// <summary>Décrémente les buffs en fin de round ; retourne les noms des combattants dont le buff expire.</summary>
    public List<string> TickRoundEnd()
    {
        var expired = new List<string>();

        foreach (var pair in new List<KeyValuePair<IBattler, ForceBuff>>(_forceBuffs))
        {
            ForceBuff buff = pair.Value;
            buff.TurnsLeft--;

            if (buff.TurnsLeft <= 0)
            {
                expired.Add(pair.Key.Name);
                _forceBuffs.Remove(pair.Key);
            }
        }

        return expired;
    }

    public bool HasForceBuff(IBattler battler) =>
        battler != null && _forceBuffs.ContainsKey(battler);

    public int GetForceBuffTurnsLeft(IBattler battler) =>
        battler != null && _forceBuffs.TryGetValue(battler, out ForceBuff buff)
            ? buff.TurnsLeft
            : 0;
}
