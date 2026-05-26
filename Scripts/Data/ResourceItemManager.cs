using System;
using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

public static class ResourceItemManager
{
    const string CatalogPath = "res://Datas/Persos/resources.csv";
    const string IconRoot = "res://Assets/UI/resources/";

    static Dictionary<string, ResourceItem> _catalog;

    public static IReadOnlyDictionary<string, ResourceItem> Catalog => EnsureLoaded();

    public static ResourceItem GetResource(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return EnsureLoaded().TryGetValue(name.Trim(), out ResourceItem resource) ? resource : null;
    }

    public static string ResolveIconPath(ResourceItem resource)
    {
        if (resource == null || string.IsNullOrWhiteSpace(resource.IconFile))
            return null;

        string icon = resource.IconFile.Trim();
        if (icon.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            return icon;

        return IconRoot + icon;
    }

    static Dictionary<string, ResourceItem> EnsureLoaded()
    {
        if (_catalog != null)
            return _catalog;

        _catalog = new Dictionary<string, ResourceItem>(StringComparer.OrdinalIgnoreCase);

        using var file = FileAccess.Open(CatalogPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[ResourceItemManager] Impossible de lire {CatalogPath}.");
            return _catalog;
        }

        file.GetLine();

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 4)
                continue;

            for (int i = 0; i < cols.Length; i++)
                cols[i] = cols[i].Trim();

            if (string.IsNullOrWhiteSpace(cols[0]))
                continue;

            var resource = new ResourceItem
            {
                Name = cols[0],
                Type = cols[1],
                IconFile = cols[2],
                Description = cols[3],
            };

            _catalog[resource.Name] = resource;
        }

        GD.Print($"[ResourceItemManager] {_catalog.Count} ressources chargées.");
        return _catalog;
    }
}
