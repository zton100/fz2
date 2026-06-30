#if UNITY_EDITOR
using System;
using EquipmentIdle.Net;
using EquipmentIdle.UI;
using UnityEditor;
using UnityEngine;

public static class EquipmentPresenterTestRunner
{
    public static void Run()
    {
        try
        {
            SortsUpgradesBeforeWeakItems();
            FormatsAffixesForPlayers();
            SummarizesEquipmentWithSlotComparison();
            ProtectsLowRarityUpgradesFromBulkDecompose();
            EstimatesEquipBestDeltaAcrossSlots();
            Debug.Log("[EquipmentPresenterTestRunner] OK");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[EquipmentPresenterTestRunner] FAIL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void SortsUpgradesBeforeWeakItems()
    {
        var currentWeapon = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var weakWeapon = Equipment("weak", 0, 0, 0, Affix("max_hp", 1, 3));
        var upgradeWeapon = Equipment("upgrade", 0, 2, 0, Affix("strength", 2, 30));
        var otherSlot = Equipment("ring", 5, 4, 0, Affix("crit_rate", 3, 0.08f));

        var sorted = EquipmentPresenter.SortBagForDisplay(
            new[] { weakWeapon, otherSlot, upgradeWeapon },
            new[] { currentWeapon });

        AssertBefore(sorted, "upgrade", "weak", "upgrade should sort before weaker same-slot item");
    }

    private static void FormatsAffixesForPlayers()
    {
        AssertEqual("Strength +12", EquipmentPresenter.FormatAffix(Affix("strength", 1, 12)), "strength affix label");
        AssertEqual("Critical Chance +8.0%", EquipmentPresenter.FormatAffix(Affix("crit_rate", 2, 0.08f)), "crit rate affix label");
    }

    private static void SummarizesEquipmentWithSlotComparison()
    {
        var current = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var upgrade = Equipment("upgrade", 0, 2, 0, Affix("strength", 2, 30));
        string summary = EquipmentPresenter.BuildDetail(upgrade, current);

        AssertContains(summary, "Score:", "detail should show score");
        AssertContains(summary, "Delta: +", "detail should show positive comparison");
        AssertContains(summary, "Strength +30", "detail should use readable affix names");
    }

    private static void ProtectsLowRarityUpgradesFromBulkDecompose()
    {
        var current = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var weakCommon = Equipment("weak-common", 0, 0, 0, Affix("max_hp", 1, 3));
        var usefulMagic = Equipment("useful-magic", 0, 1, 0, Affix("strength", 2, 30));
        var rare = Equipment("rare", 0, 2, 0, Affix("strength", 1, 1));

        var decompose = EquipmentPresenter.BulkDecomposeCandidates(
            new[] { weakCommon, usefulMagic, rare },
            new[] { current });

        AssertContainsUid(decompose, "weak-common", "weak low-rarity item should be decomposed");
        AssertMissingUid(decompose, "useful-magic", "low-rarity upgrade should be protected");
        AssertMissingUid(decompose, "rare", "rare item should be protected");
    }

    private static void EstimatesEquipBestDeltaAcrossSlots()
    {
        var currentWeapon = Equipment("current-weapon", 0, 1, 0, Affix("strength", 1, 5));
        var currentHelm = Equipment("current-helm", 1, 0, 0, Affix("armor", 1, 3));
        var betterWeapon = Equipment("better-weapon", 0, 2, 0, Affix("strength", 2, 30));
        var worseHelm = Equipment("worse-helm", 1, 0, 0, Affix("armor", 1, 1));

        float delta = EquipmentPresenter.EquipBestDelta(
            new[] { betterWeapon, worseHelm },
            new[] { currentWeapon, currentHelm });

        if (delta <= 0f) throw new Exception($"equip best delta should be positive, got {delta}");
    }

    private static EquipmentDTO Equipment(string uid, int slot, int rarity, int upgrade, params AffixData[] affixes)
    {
        return new EquipmentDTO
        {
            uid = uid,
            name = uid,
            slot = slot,
            rarity = rarity,
            upgrade = upgrade,
            affixes = affixes,
        };
    }

    private static AffixData Affix(string type, int tier, float value)
    {
        return new AffixData { type = type, tier = tier, value = value };
    }

    private static void AssertEqual(string want, string got, string message)
    {
        if (want != got) throw new Exception($"{message}: got {got}, want {want}");
    }

    private static void AssertBefore(System.Collections.Generic.IList<EquipmentDTO> sorted, string earlier, string later, string message)
    {
        int earlierIndex = -1;
        int laterIndex = -1;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].uid == earlier) earlierIndex = i;
            if (sorted[i].uid == later) laterIndex = i;
        }
        if (earlierIndex < 0 || laterIndex < 0 || earlierIndex >= laterIndex)
            throw new Exception($"{message}: {earlier} index={earlierIndex}, {later} index={laterIndex}");
    }

    private static void AssertContains(string text, string expected, string message)
    {
        if (text == null || !text.Contains(expected))
            throw new Exception($"{message}: missing {expected} in {text}");
    }

    private static void AssertContainsUid(System.Collections.Generic.IList<EquipmentDTO> items, string uid, string message)
    {
        foreach (var item in items)
        {
            if (item.uid == uid) return;
        }
        throw new Exception($"{message}: missing {uid}");
    }

    private static void AssertMissingUid(System.Collections.Generic.IList<EquipmentDTO> items, string uid, string message)
    {
        foreach (var item in items)
        {
            if (item.uid == uid) throw new Exception($"{message}: found {uid}");
        }
    }
}
#endif
