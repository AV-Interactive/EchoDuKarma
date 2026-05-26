using System;
using System.Globalization;
using System.Text.RegularExpressions;
using EchoduKarma.Scripts.Data;
using Godot;

/// <summary>
/// Évalue les tokens CONDITION ACCES des dialogues (séparés par |, logique ET).
/// Formats : interaction joueur, QUEST_ACTIVE:id, KARMA:>=10, KARMA:Introduction:>=10
/// </summary>
public static class DialogueConditions
{
    static readonly Regex KarmaOpRegex = new(
        @"^(>=|<=|>|<|==|!=)(-?\d+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool EvaluateAll(string combined)
    {
        if (string.IsNullOrWhiteSpace(combined))
            return true;

        foreach (string raw in combined.Split('|'))
        {
            string token = raw.Trim();
            if (string.IsNullOrEmpty(token))
                continue;

            if (!EvaluateToken(token))
                return false;
        }

        return true;
    }

    public static string GetFailureReason(string combined)
    {
        if (string.IsNullOrWhiteSpace(combined))
            return null;

        foreach (string raw in combined.Split('|'))
        {
            string token = raw.Trim();
            if (string.IsNullOrEmpty(token) || EvaluateToken(token))
                continue;

            return DescribeFailedToken(token);
        }

        return null;
    }

    static bool EvaluateToken(string token)
    {
        if (token.Equals("interaction joueur", StringComparison.OrdinalIgnoreCase)
            || token.Equals("proximité joueur", StringComparison.OrdinalIgnoreCase))
            return true;

        if (token.StartsWith("QUEST_", StringComparison.OrdinalIgnoreCase))
            return QuestManager.Instance?.CheckCondition(token) ?? true;

        if (token.StartsWith("KARMA:", StringComparison.OrdinalIgnoreCase))
            return EvaluateKarma(token);

        return true;
    }

    static bool EvaluateKarma(string token)
    {
        if (!TryParseKarmaToken(token, out string zone, out string op, out int threshold))
            return true;

        float current = KarmaManager.Instance?.GetZoneKarma(zone) ?? 0f;
        return Compare(current, op, threshold);
    }

    static bool TryParseKarmaToken(string token, out string zone, out string op, out int threshold)
    {
        zone = ResolveZone(null);
        op = null;
        threshold = 0;

        if (!token.StartsWith("KARMA:", StringComparison.OrdinalIgnoreCase))
            return false;

        string[] parts = token.Split(':');
        if (parts.Length < 2)
            return false;

        string opPart = parts[^1].Trim();
        if (!KarmaOpRegex.IsMatch(opPart))
            return false;

        var match = KarmaOpRegex.Match(opPart);
        op = match.Groups[1].Value;
        threshold = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (parts.Length >= 3)
            zone = ResolveZone(parts[1].Trim());

        return true;
    }

    static string ResolveZone(string zoneOverride)
    {
        if (!string.IsNullOrWhiteSpace(zoneOverride))
            return zoneOverride.Trim();

        if (KarmaManager.Instance is not null && !string.IsNullOrWhiteSpace(KarmaManager.Instance.CurrentZone))
            return KarmaManager.Instance.CurrentZone;

        return GameManager.Instance?.ReturnZoneName ?? "Introduction";
    }

    static bool Compare(float value, string op, int threshold) => op switch
    {
        ">=" => value >= threshold,
        "<=" => value <= threshold,
        ">"  => value > threshold,
        "<"  => value < threshold,
        "==" => value == threshold,
        "!=" => value != threshold,
        _    => true,
    };

    static string DescribeFailedToken(string token)
    {
        if (token.StartsWith("KARMA:", StringComparison.OrdinalIgnoreCase)
            && TryParseKarmaToken(token, out string zone, out string op, out int threshold))
        {
            float current = KarmaManager.Instance?.GetZoneKarma(zone) ?? 0f;
            string requirement = FormatKarmaRequirement(op, threshold);
            string state = KarmaManager.GetStateLabel(current);
            return $"[color=#E85D5D]Condition non remplie[/color] : le Karma de [color=#3F9DD9]{zone}[/color] doit être {requirement}.\n" +
                   $"Actuellement : [color=#FFD166]{KarmaManager.FormatKarma(current)}[/color] ({state}).";
        }

        if (token.StartsWith("QUEST_", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = token.Split(':');
            string questId = parts.Length > 1 ? parts[1] : "?";
            return $"[color=#E85D5D]Condition non remplie[/color] : {FormatQuestRequirement(token)} ([color=#FFD166]{questId}[/color]).";
        }

        return "[color=#E85D5D]Condition non remplie[/color] : tu ne remplis pas encore les critères pour cette option.";
    }

    static string FormatKarmaRequirement(string op, int threshold) => op switch
    {
        ">=" => $"d'[color=#58B4C6]au moins {threshold}[/color]",
        "<=" => $"d'[color=#58B4C6]au plus {threshold}[/color]",
        ">"  => $"[color=#58B4C6]strictement supérieur à {threshold}[/color]",
        "<"  => $"[color=#58B4C6]strictement inférieur à {threshold}[/color]",
        "==" => $"[color=#58B4C6]égal à {threshold}[/color]",
        "!=" => $"[color=#58B4C6]différent de {threshold}[/color]",
        _    => $"[color=#58B4C6]{threshold}[/color]",
    };

    static string FormatQuestRequirement(string token)
    {
        string[] parts = token.Split(':');
        if (parts.Length < 2)
            return "objectif de quête requis";

        return parts[0].Trim() switch
        {
            "QUEST_ACTIVE"   => "quête en cours",
            "QUEST_DONE"     => "quête terminée",
            "QUEST_INACTIVE" => "quête non commencée",
            _                => "état de quête requis",
        };
    }
}
