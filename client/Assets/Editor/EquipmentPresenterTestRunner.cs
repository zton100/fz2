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
            OffersDetailActionsByEquipmentLocation();
            DescribesDungeonStateWithServerMonsterCurve();
            RecommendsNextGoalFromProgressState();
            LabelsEquipmentRowsWithUpgradeContext();
            LabelsTalentRowsWithUpgradeState();
            HighlightsRareLootDrops();
            DescribesBossDistanceAndRewards();
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
        AssertEqual("力量 +12", EquipmentPresenter.FormatAffix(Affix("strength", 1, 12)), "strength affix label");
        AssertEqual("暴击率 +8.0%", EquipmentPresenter.FormatAffix(Affix("crit_rate", 2, 0.08f)), "crit rate affix label");
    }

    private static void SummarizesEquipmentWithSlotComparison()
    {
        var current = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var upgrade = Equipment("upgrade", 0, 2, 0, Affix("strength", 2, 30));
        string summary = EquipmentPresenter.BuildDetail(upgrade, current);

        AssertContains(summary, "评分：", "detail should show score");
        AssertContains(summary, "对比：+", "detail should show positive comparison");
        AssertContains(summary, "力量 +30", "detail should use readable affix names");
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

    private static void OffersDetailActionsByEquipmentLocation()
    {
        var selected = Equipment("selected", 0, 1, 0, Affix("strength", 1, 5));

        var bagActions = EquipmentPresenter.DetailActions(selected, false);
        AssertContainsAction(bagActions, "equip", "bag item should offer equip");
        AssertContainsAction(bagActions, "upgrade", "bag item should offer upgrade");
        AssertContainsAction(bagActions, "reforge", "bag item should offer reforge");
        AssertContainsAction(bagActions, "decompose", "bag item should offer decompose");
        AssertMissingAction(bagActions, "unequip", "bag item should not offer unequip");

        var equippedActions = EquipmentPresenter.DetailActions(selected, true);
        AssertContainsAction(equippedActions, "unequip", "equipped item should offer unequip");
        AssertMissingAction(equippedActions, "equip", "equipped item should not offer equip");

        var emptyActions = EquipmentPresenter.DetailActions(null, false);
        if (emptyActions.Count != 0) throw new Exception("empty detail should not offer actions");
    }

    private static void DescribesDungeonStateWithServerMonsterCurve()
    {
        var floor21 = EquipmentPresenter.BuildDungeonState(21, 120f);
        var floor41 = EquipmentPresenter.BuildDungeonState(41, 120f);
        if (floor41.MonsterPower / floor21.MonsterPower < 2.3f)
            throw new Exception($"monster curve should accelerate after floor 20, got ratio {floor41.MonsterPower / floor21.MonsterPower:F2}");

        var boss = EquipmentPresenter.BuildDungeonState(10, 10f);
        AssertContains(boss.Title, "Boss 关", "boss floor should be labeled");
        AssertContains(boss.Monster, "守层 Boss", "boss encounter should name boss");
        AssertContains(boss.Battle, "战力不足", "weak hero should show blocked state");
        AssertNear(1f, boss.GateProgress, 0.001f, "boss gate should be full at every fifth floor");

        var clear = EquipmentPresenter.BuildDungeonState(2, 20f);
        AssertContains(clear.Battle, "优势", "strong hero should show winning state");
    }

    private static void RecommendsNextGoalFromProgressState()
    {
        string reincarn = EquipmentPresenter.BuildNextGoal(10, 100f, true, 0, 0);
        AssertContains(reincarn, "转生", "reincarnation should be the highest priority goal");

        string blocked = EquipmentPresenter.BuildNextGoal(4, 5f, false, 3, 12);
        AssertContains(blocked, "穿戴", "underpowered hero with bag upgrades should be guided to equipment");

        string clearing = EquipmentPresenter.BuildNextGoal(2, 30f, false, 0, 0);
        AssertContains(clearing, "自动战斗", "strong hero should show active progress");
    }

    private static void LabelsEquipmentRowsWithUpgradeContext()
    {
        var current = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var upgrade = Equipment("upgrade", 0, 2, 0, Affix("strength", 2, 30));
        var weaker = Equipment("weaker", 0, 0, 0, Affix("max_hp", 1, 3));
        var newSlot = Equipment("new-slot", 1, 0, 0, Affix("armor", 1, 3));

        AssertContains(EquipmentPresenter.BuildEquipmentLine(upgrade, current, false), "提升 +", "upgrade row should show positive delta");
        AssertContains(EquipmentPresenter.BuildEquipmentLine(weaker, current, false), "更弱", "weaker row should show negative context");
        AssertContains(EquipmentPresenter.BuildEquipmentLine(newSlot, null, false), "新部位", "empty slot item should be called out");
        AssertContains(EquipmentPresenter.BuildEquipmentLine(current, null, true), "已穿戴", "equipped row should show equipped state");
    }

    private static void LabelsTalentRowsWithUpgradeState()
    {
        AssertContains(EquipmentPresenter.BuildTalentLine("伤害", 0, 10, "每级伤害 +5%", 1), "可升级", "talent with souls should show upgrade available");
        AssertContains(EquipmentPresenter.BuildTalentLine("品质", 3, 3, "品质下限", 5), "满级", "max talent should show max state");
        AssertContains(EquipmentPresenter.BuildTalentLine("掉落", 1, 10, "高稀有度权重", 0), "需要魂点", "talent without souls should show soul requirement");
    }

    private static void HighlightsRareLootDrops()
    {
        var common = Equipment("common", 0, 0, 0, Affix("strength", 1, 5));
        var rare = Equipment("rare", 0, 2, 0, Affix("strength", 2, 30));
        var legendary = Equipment("legendary", 0, 3, 0, Affix("strength", 3, 50));

        AssertContains(EquipmentPresenter.BuildLootLine(common), "普通", "common loot should show rarity");
        AssertContains(EquipmentPresenter.BuildLootLine(rare), "稀有掉落", "rare loot should be highlighted");
        AssertContains(EquipmentPresenter.BuildLootToast(rare), "稀有掉落", "rare loot toast should be highlighted");
        AssertContains(EquipmentPresenter.BuildLootToast(legendary), "传奇掉落", "legendary loot toast should have its own emphasis");
    }

    private static void DescribesBossDistanceAndRewards()
    {
        var beforeBoss = EquipmentPresenter.BuildDungeonState(4, 100f);
        AssertContains(beforeBoss.BossHint, "距 Boss 关还差 1 层", "pre-boss hint should show distance");
        AssertContains(beforeBoss.BossHint, "基础材料 +10", "pre-boss hint should show next reward");

        var boss = EquipmentPresenter.BuildDungeonState(5, 100f);
        AssertContains(boss.BossHint, "首通奖励：基础材料 +10", "boss hint should show current reward");
        if (boss.BossReward != 10) throw new Exception($"boss reward should match server rule, got {boss.BossReward}");
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

    private static void AssertNear(float want, float got, float tolerance, string message)
    {
        if (Math.Abs(want - got) > tolerance)
            throw new Exception($"{message}: got {got}, want {want}");
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

    private static void AssertContainsAction(System.Collections.Generic.IList<EquipmentAction> actions, string id, string message)
    {
        foreach (var action in actions)
        {
            if (action.Id == id) return;
        }
        throw new Exception($"{message}: missing {id}");
    }

    private static void AssertMissingAction(System.Collections.Generic.IList<EquipmentAction> actions, string id, string message)
    {
        foreach (var action in actions)
        {
            if (action.Id == id) throw new Exception($"{message}: found {id}");
        }
    }
}
#endif
