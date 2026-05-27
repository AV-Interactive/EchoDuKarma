using Godot;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Autoload — jauge de Karma par zone (-100 à +100, pas de 0,15).
/// </summary>
public partial class KarmaManager : Node
{
    public const float MinKarma = -100f;
    public const float MaxKarma = 100f;

    /// <summary>Perte de karma par monstre vaincu en combat (zone courante).</summary>
    public const float KarmaLossPerMonsterKill = -0.15f;

    public static KarmaManager Instance { get; private set; }

    readonly Dictionary<string, float> _zoneKarma = new();

    public string CurrentZone { get; private set; } = "Introduction";

    [Signal] public delegate void KarmaChangedEventHandler(string zone, float newValue, float delta);
    [Signal] public delegate void CurrentZoneChangedEventHandler(string zone);

    public override void _Ready()
    {
        Instance = this;
        EnsureZoneInitialized("Introduction");
        SetCurrentZone("Introduction");
        float k = GetZoneKarma(CurrentZone);
        GD.Print($"[KarmaManager] Zone '{CurrentZone}' : {FormatKarma(k)} ({GetStateLabel(k)})");
    }

    public void SetCurrentZone(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
            return;

        zone = zone.Trim();
        EnsureZoneInitialized(zone);

        if (CurrentZone == zone)
            return;

        CurrentZone = zone;
        EmitSignal(SignalName.CurrentZoneChanged, zone);
        GD.Print($"[KarmaManager] Zone active : {zone} ({FormatKarma(GetZoneKarma(zone))})");
    }

    public float GetZoneKarma(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
            zone = CurrentZone;

        EnsureZoneInitialized(zone);
        return _zoneKarma[zone];
    }

    public void ApplyKarmaImpact(string zone, float delta)
    {
        if (Mathf.IsZeroApprox(delta))
            return;

        if (string.IsNullOrWhiteSpace(zone))
            zone = CurrentZone;

        EnsureZoneInitialized(zone);
        float newValue = Clamp(_zoneKarma[zone] + delta);
        _zoneKarma[zone] = newValue;

        EmitSignal(SignalName.KarmaChanged, zone, newValue, delta);
        GD.Print($"[KarmaManager] [{zone}] Karma {FormatDelta(delta)} → {FormatKarma(newValue)} ({GetStateLabel(newValue)})");
    }

    public void ApplyMonsterKillImpact(string zone = null)
        => ApplyKarmaImpact(zone, KarmaLossPerMonsterKill);

    void EnsureZoneInitialized(string zone)
    {
        if (_zoneKarma.ContainsKey(zone))
            return;

        _zoneKarma[zone] = zone == "Introduction" ? 15f : 0f;
    }

    public static string GetStateLabel(float karma)
    {
        karma = Clamp(karma);
        if (karma >= 70f) return "Utopie étouffante";
        if (karma >= 30f) return "Ordre Stable";
        if (karma >= -20f) return "Équilibre";
        if (karma >= -69f) return "Instabilité";
        return "Chaos total";
    }

    public static float Clamp(float value) => Mathf.Clamp(value, MinKarma, MaxKarma);

    /// <summary>Position normalisée 0..1 sur la jauge (-100 = 0, 0 = 0.5, +100 = 1).</summary>
    public static float KarmaToNormalized(float karma)
        => (Clamp(karma) - MinKarma) / (MaxKarma - MinKarma);

    public static string FormatKarma(float value)
        => Clamp(value).ToString("0.##", CultureInfo.InvariantCulture);

    public static string FormatDelta(float delta)
    {
        string sign = delta > 0f ? "+" : "";
        return sign + delta.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public Dictionary<string, float> ExportZoneKarma() => new(_zoneKarma);

    public void ImportZoneKarma(IReadOnlyDictionary<string, float> values, string currentZone)
    {
        _zoneKarma.Clear();

        if (values != null)
        {
            foreach (var pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                _zoneKarma[pair.Key.Trim()] = Clamp(pair.Value);
            }
        }

        EnsureZoneInitialized("Introduction");
        SetCurrentZone(string.IsNullOrWhiteSpace(currentZone) ? "Introduction" : currentZone.Trim());
    }
}
