using System;
using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

public sealed class HeroData
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string ClassName { get; init; }
    public ElementType Affinity { get; init; }
}

/// <summary>Charge res://Datas/Persos/heroes.csv (source d'affinité et de classe du joueur).</summary>
public static class HeroManager
{
    const string CatalogPath = "res://Datas/Persos/heroes.csv";
    const int DefaultHeroId = 1;

    static Dictionary<int, HeroData> _heroesById;
    static Dictionary<string, HeroData> _heroesByName;

    public static HeroData GetHero(int id)
    {
        EnsureLoaded();
        return _heroesById.GetValueOrDefault(id);
    }

    public static HeroData GetHero(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        EnsureLoaded();
        return _heroesByName.GetValueOrDefault(name.Trim());
    }

    public static HeroData GetDefaultHero() => GetHero(DefaultHeroId);

    static void EnsureLoaded()
    {
        if (_heroesById != null)
            return;

        _heroesById = new Dictionary<int, HeroData>();
        _heroesByName = new Dictionary<string, HeroData>(StringComparer.OrdinalIgnoreCase);

        using var file = FileAccess.Open(CatalogPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[HeroManager] Impossible de lire {CatalogPath}.");
            return;
        }

        file.GetLine();

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 4)
                continue;

            for (int i = 0; i < cols.Length; i++)
                cols[i] = cols[i].Trim();

            if (!int.TryParse(cols[0], out int id))
                continue;

            var hero = new HeroData
            {
                Id = id,
                Name = cols[1],
                ClassName = cols[2],
                Affinity = ElementCombat.Parse(cols[3]),
            };

            _heroesById[id] = hero;
            _heroesByName[hero.Name] = hero;
        }

        GD.Print($"[HeroManager] {_heroesById.Count} héros chargés.");
    }
}
