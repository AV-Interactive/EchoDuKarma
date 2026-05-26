using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.Data;

public static class EquipmentManager
{
    const string CatalogPath = "res://Datas/Persos/equipments.csv";

    static Dictionary<string, Equipment> _catalog;

    public static IReadOnlyDictionary<string, Equipment> Catalog => EnsureLoaded();

    public static Equipment GetEquipment(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return EnsureLoaded().TryGetValue(name.Trim(), out Equipment equipment) ? equipment : null;
    }

    static Dictionary<string, Equipment> EnsureLoaded()
    {
        if (_catalog != null)
            return _catalog;

        _catalog = new Dictionary<string, Equipment>(StringComparer.OrdinalIgnoreCase);

        using var file = FileAccess.Open(CatalogPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[EquipmentManager] Impossible de lire {CatalogPath}.");
            return _catalog;
        }

        file.GetLine();

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 10)
                continue;

            for (int i = 0; i < cols.Length; i++)
                cols[i] = cols[i].Trim();

            if (string.IsNullOrWhiteSpace(cols[0]))
                continue;

            if (!Equipment.TryParseSlot(cols[1], out EquipmentSlot slot))
            {
                GD.PrintErr($"[EquipmentManager] Slot invalide pour '{cols[0]}' : '{cols[1]}'.");
                continue;
            }

            var equipment = new Equipment
            {
                Name = cols[0],
                Slot = slot,
                Type = cols[2],
                Strength = ParseInt(cols[3]),
                Dexterity = ParseInt(cols[4]),
                Spirit = ParseInt(cols[5]),
                Defense = ParseInt(cols[6]),
                PassiveAbility = cols[7],
                Classes = ParseClasses(cols[8]),
                Price = ParseInt(cols[9]),
            };

            _catalog[equipment.Name] = equipment;
        }

        GD.Print($"[EquipmentManager] {_catalog.Count} équipements chargés.");
        return _catalog;
    }

    static int ParseInt(string raw) =>
        int.TryParse(raw?.Trim(), out int value) ? value : 0;

    static List<string> ParseClasses(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return list;

        foreach (string part in raw.Split(',', '|'))
        {
            string cls = part.Trim();
            if (!string.IsNullOrEmpty(cls))
                list.Add(cls);
        }

        return list;
    }
}
