using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.Data;

public readonly struct ShopOffer
{
    public string ShopId { get; init; }
    public string MerchantName { get; init; }
    public Equipment Equipment { get; init; }
}

public static class ShopCatalog
{
    const string CatalogPath = "res://Datas/Progress/shops.csv";

    static Dictionary<string, List<ShopOffer>> _shopsById;

    public static IReadOnlyList<ShopOffer> GetShopOffers(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId))
            return Array.Empty<ShopOffer>();

        return EnsureLoaded().TryGetValue(shopId.Trim(), out List<ShopOffer> offers)
            ? offers
            : Array.Empty<ShopOffer>();
    }

    public static string GetMerchantName(string shopId)
    {
        var offers = GetShopOffers(shopId);
        return offers.Count > 0 ? offers[0].MerchantName : "Marchand";
    }

    static Dictionary<string, List<ShopOffer>> EnsureLoaded()
    {
        if (_shopsById != null)
            return _shopsById;

        _shopsById = new Dictionary<string, List<ShopOffer>>(StringComparer.OrdinalIgnoreCase);

        using var file = FileAccess.Open(CatalogPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[ShopCatalog] Impossible de lire {CatalogPath}.");
            return _shopsById;
        }

        file.GetLine();

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 3)
                continue;

            string shopId = cols[0].Trim();
            string merchantName = cols[1].Trim();
            string equipmentName = cols[2].Trim();

            if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(equipmentName))
                continue;

            Equipment equipment = EquipmentManager.GetEquipment(equipmentName);
            if (equipment == null)
            {
                GD.PrintErr($"[ShopCatalog] Équipement inconnu '{equipmentName}' pour la boutique '{shopId}'.");
                continue;
            }

            if (!_shopsById.TryGetValue(shopId, out List<ShopOffer> list))
            {
                list = new List<ShopOffer>();
                _shopsById[shopId] = list;
            }

            list.Add(new ShopOffer
            {
                ShopId = shopId,
                MerchantName = string.IsNullOrWhiteSpace(merchantName) ? "Marchand" : merchantName,
                Equipment = equipment,
            });
        }

        GD.Print($"[ShopCatalog] {_shopsById.Count} boutique(s) chargée(s).");
        return _shopsById;
    }
}
