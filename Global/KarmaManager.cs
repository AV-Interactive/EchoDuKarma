using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload — jauge de Karma par zone (-100 à +100).
/// </summary>
public partial class KarmaManager : Node
{
    public const int MinKarma = -100;
    public const int MaxKarma = 100;

    public static KarmaManager Instance { get; private set; }

    readonly Dictionary<string, int> _zoneKarma = new();

    public string CurrentZone { get; private set; } = "Introduction";

    [Signal] public delegate void KarmaChangedEventHandler(string zone, int newValue, int delta);
    [Signal] public delegate void CurrentZoneChangedEventHandler(string zone);

    public override void _Ready()
    {
        Instance = this;
        EnsureZoneInitialized("Introduction");
        SetCurrentZone("Introduction");
        GD.Print($"[KarmaManager] Zone '{CurrentZone}' : {GetZoneKarma(CurrentZone)} ({GetStateLabel(GetZoneKarma(CurrentZone))})");
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
        GD.Print($"[KarmaManager] Zone active : {zone} ({GetZoneKarma(zone)})");
    }

    public int GetZoneKarma(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
            zone = CurrentZone;

        EnsureZoneInitialized(zone);
        return _zoneKarma[zone];
    }

    public void ApplyKarmaImpact(string zone, int delta)
    {
        if (delta == 0)
            return;

        if (string.IsNullOrWhiteSpace(zone))
            zone = CurrentZone;

        EnsureZoneInitialized(zone);
        int newValue = Clamp(_zoneKarma[zone] + delta);
        _zoneKarma[zone] = newValue;

        EmitSignal(SignalName.KarmaChanged, zone, newValue, delta);
        GD.Print($"[KarmaManager] [{zone}] Karma {(delta >= 0 ? "+" : "")}{delta} → {newValue} ({GetStateLabel(newValue)})");
    }

    void EnsureZoneInitialized(string zone)
    {
        if (_zoneKarma.ContainsKey(zone))
            return;

        _zoneKarma[zone] = zone == "Introduction" ? 15 : 0;
    }

    public static string GetStateLabel(int karma)
    {
        if (karma >= 70) return "Utopie étouffante";
        if (karma >= 30) return "Ordre Stable";
        if (karma >= -20) return "Équilibre";
        if (karma >= -69) return "Instabilité";
        return "Chaos total";
    }

    public static int Clamp(int value) => Mathf.Clamp(value, MinKarma, MaxKarma);

    /// <summary>Position normalisée 0..1 sur la jauge (-100 = 0, 0 = 0.5, +100 = 1).</summary>
    public static float KarmaToNormalized(int karma)
        => (Clamp(karma) - MinKarma) / (float)(MaxKarma - MinKarma);
}
