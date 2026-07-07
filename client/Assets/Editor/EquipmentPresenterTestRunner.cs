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
            FiltersBagItemsByUserModeAndLockState();
            ProtectsLowRarityUpgradesFromBulkDecompose();
            EstimatesEquipBestDeltaAcrossSlots();
            OffersDetailActionsByEquipmentLocation();
            OffersUpgradeInheritanceForSameSlotNewGear();
            EncodesLockEquipmentRequests();
            EncodesTransferUpgradeRequests();
            DescribesDungeonStateWithServerMonsterCurve();
            RecommendsNextGoalFromProgressState();
            LabelsEquipmentRowsWithUpgradeContext();
            BuildsStructuredEquipmentComparisonRows();
            LabelsTalentRowsWithUpgradeState();
            HighlightsRareLootDrops();
            BuildsLootAndBossCeremonyText();
            DescribesBossDistanceAndRewards();
            BuildsProgressNodesForBossCycle();
            BuildsCraftAndReincarnationPlans();
            BuildsBattleStageStateForVisualCombat();
            BuildsCombatBeatStateForHitFeedback();
            NamesDungeonZonesAndMonsters();
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

    private static void FiltersBagItemsByUserModeAndLockState()
    {
        var current = Equipment("current", 0, 1, 0, Affix("strength", 1, 5));
        var weakCommon = Equipment("weak-common", 0, 0, 0, Affix("max_hp", 1, 3));
        var usefulMagic = Equipment("useful-magic", 0, 1, 0, Affix("strength", 2, 30));
        var rare = Equipment("rare", 0, 2, 0, Affix("strength", 1, 1));

        if (!EquipmentPresenter.ShouldShowInBag(weakCommon, current, EquipmentBagFilter.All, false))
            throw new Exception("all filter should show ordinary bag items");
        if (!EquipmentPresenter.ShouldShowInBag(usefulMagic, current, EquipmentBagFilter.Upgrades, false))
            throw new Exception("upgrade filter should show positive upgrades");
        if (EquipmentPresenter.ShouldShowInBag(weakCommon, current, EquipmentBagFilter.Upgrades, false))
            throw new Exception("upgrade filter should hide weaker items");
        if (!EquipmentPresenter.ShouldShowInBag(rare, current, EquipmentBagFilter.Rare, false))
            throw new Exception("rare filter should show rarity 2+ items");
        if (!EquipmentPresenter.ShouldShowInBag(weakCommon, current, EquipmentBagFilter.Decompose, false))
            throw new Exception("decompose filter should show unlocked weak common items");
        if (EquipmentPresenter.ShouldShowInBag(weakCommon, current, EquipmentBagFilter.Decompose, true))
            throw new Exception("decompose filter should hide locked items");
        if (EquipmentPresenter.ShouldShowInBag(usefulMagic, current, EquipmentBagFilter.Decompose, false))
            throw new Exception("decompose filter should hide useful upgrades");
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

        var bagActions = EquipmentPresenter.DetailActions(selected, false, null);
        AssertContainsAction(bagActions, "equip", "bag item should offer equip");
        AssertContainsAction(bagActions, "upgrade", "bag item should offer upgrade");
        AssertContainsAction(bagActions, "reforge", "bag item should offer reforge");
        AssertContainsAction(bagActions, "decompose", "bag item should offer decompose");
        AssertMissingAction(bagActions, "unequip", "bag item should not offer unequip");

        var equippedActions = EquipmentPresenter.DetailActions(selected, true, null);
        AssertContainsAction(equippedActions, "unequip", "equipped item should offer unequip");
        AssertMissingAction(equippedActions, "equip", "equipped item should not offer equip");

        var emptyActions = EquipmentPresenter.DetailActions(null, false, null);
        if (emptyActions.Count != 0) throw new Exception("empty detail should not offer actions");
    }

    private static void OffersUpgradeInheritanceForSameSlotNewGear()
    {
        var current = Equipment("current", 0, 1, 6, Affix("strength", 1, 5));
        var target = Equipment("target", 0, 2, 1, Affix("strength", 2, 30));
        var otherSlot = Equipment("other", 1, 2, 1, Affix("armor", 2, 30));

        var actions = EquipmentPresenter.DetailActions(target, false, current);
        AssertContainsAction(actions, "transfer_upgrade", "same-slot lower-upgrade target should offer inheritance");
        if (!EquipmentPresenter.CanInheritUpgrade(current, target))
            throw new Exception("same-slot higher source should be inheritable");
        AssertContains(EquipmentPresenter.BuildTransferLine(current, target), "继承强化", "transfer line should explain inheritance");
        AssertContains(EquipmentPresenter.BuildTransferLine(current, target), "评分", "transfer line should preview score change");
        if (target.upgrade != 1) throw new Exception($"transfer preview should not mutate target upgrade, got {target.upgrade}");
        AssertContainsComparison(EquipmentPresenter.BuildTransferPreviewRows(current, target), "继承后评分", true, "transfer preview should include target score gain");
        AssertContainsComparison(EquipmentPresenter.BuildTransferPreviewRows(current, target), "旧装回退", false, "transfer preview should include source rollback");

        var noTransfer = EquipmentPresenter.DetailActions(otherSlot, false, current);
        AssertMissingAction(noTransfer, "transfer_upgrade", "different-slot target should not offer inheritance");
    }

    private static void EncodesLockEquipmentRequests()
    {
        string encoded = Message.EncodeLockEquipment("r1", "eq\"locked", true);
        AssertContains(encoded, "\"t\":\"lock_equipment\"", "lock request should use lock_equipment type");
        AssertContains(encoded, "\"uid\":\"eq\\\"locked\"", "lock request should escape equipment uid");
        AssertContains(encoded, "\"locked\":true", "lock request should carry locked=true");

        var parsed = Message.Parse(encoded);
        AssertEqual(Message.TypeLockEquipment, parsed.t, "parsed lock request type");
        AssertContains(parsed.dataJson, "\"locked\":true", "parsed lock request data should preserve locked flag");
    }

    private static void EncodesTransferUpgradeRequests()
    {
        string encoded = Message.EncodeTransferUpgrade("r2", "old\"weapon", "new_weapon");
        AssertContains(encoded, "\"t\":\"transfer_upgrade\"", "transfer request should use transfer_upgrade type");
        AssertContains(encoded, "\"source_uid\":\"old\\\"weapon\"", "transfer request should escape source uid");
        AssertContains(encoded, "\"target_uid\":\"new_weapon\"", "transfer request should include target uid");

        var parsed = Message.Parse(encoded);
        AssertEqual(Message.TypeTransferUpgrade, parsed.t, "parsed transfer request type");
        AssertContains(parsed.dataJson, "\"source_uid\"", "parsed transfer request data should preserve source uid");
    }

    private static void DescribesDungeonStateWithServerMonsterCurve()
    {
        var floor21 = EquipmentPresenter.BuildDungeonState(21, 120f);
        var floor41 = EquipmentPresenter.BuildDungeonState(41, 120f);
        if (floor41.MonsterPower / floor21.MonsterPower < 2.3f)
            throw new Exception($"monster curve should accelerate after floor 20, got ratio {floor41.MonsterPower / floor21.MonsterPower:F2}");

        var floor81 = EquipmentPresenter.BuildDungeonState(81, 500f);
        var floor121 = EquipmentPresenter.BuildDungeonState(121, 500f);
        if (floor121.MonsterPower / floor81.MonsterPower < 7.5f)
            throw new Exception($"monster curve should accelerate again after floor 80, got ratio {floor121.MonsterPower / floor81.MonsterPower:F2}");

        var floor159 = EquipmentPresenter.BuildDungeonState(159, 500f);
        float frontierRatio = floor159.MonsterPower / floor121.MonsterPower;
        if (frontierRatio < 2.3f || frontierRatio > 3.2f)
            throw new Exception($"monster curve should taper after floor 120, got ratio {frontierRatio:F2}");

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

    private static void BuildsStructuredEquipmentComparisonRows()
    {
        var current = Equipment("current", 0, 1, 1, Affix("strength", 1, 5), Affix("armor", 1, 3));
        var upgrade = Equipment("upgrade", 0, 2, 2, Affix("strength", 2, 30), Affix("crit_rate", 2, 0.08f));

        var rows = EquipmentPresenter.BuildComparisonRows(upgrade, current);

        AssertContainsComparison(rows, "评分", true, "comparison should include score gain");
        AssertContainsComparison(rows, "力量", true, "comparison should include selected affix gain");
        AssertContainsComparison(rows, "护甲", false, "comparison should include lost current affix");
        AssertContains(EquipmentPresenter.BuildSelectedSummary(upgrade), "评分", "selected summary should show score");
        AssertContains(EquipmentPresenter.BuildCurrentSummary(current), "current", "current summary should show equipped item name");
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

    private static void BuildsLootAndBossCeremonyText()
    {
        var common = Equipment("common", 0, 0, 0, Affix("strength", 1, 5));
        var rare = Equipment("rare", 0, 2, 0, Affix("strength", 2, 30));
        var legendary = Equipment("legendary", 0, 3, 0, Affix("strength", 3, 50));

        AssertEqual("", EquipmentPresenter.BuildLootCeremonyText(common, false), "weak common loot should not trigger ceremony");
        AssertContains(EquipmentPresenter.BuildLootCeremonyText(common, true), "可穿戴提升", "upgrade common loot should trigger ceremony");
        AssertContains(EquipmentPresenter.BuildLootCeremonyText(rare, false), "稀有掉落", "rare loot should trigger ceremony");
        AssertContains(EquipmentPresenter.BuildLootCeremonyText(legendary, false), "传奇 掉落", "legendary loot should trigger ceremony");
        AssertContains(EquipmentPresenter.BuildBossClearBanner(10), "基础材料 +30", "boss clear banner should show reward");
        AssertEqual("", EquipmentPresenter.BuildBossClearBanner(11), "normal floor should not show boss clear banner");
    }

    private static void DescribesBossDistanceAndRewards()
    {
        var beforeBoss = EquipmentPresenter.BuildDungeonState(4, 100f);
        AssertContains(beforeBoss.BossHint, "距 Boss 关还差 1 层", "pre-boss hint should show distance");
        AssertContains(beforeBoss.BossHint, "基础材料 +15", "pre-boss hint should show next reward");

        var boss = EquipmentPresenter.BuildDungeonState(5, 100f);
        AssertContains(boss.BossHint, "首通奖励：基础材料 +15", "boss hint should show current reward");
        if (boss.BossReward != 15) throw new Exception($"boss reward should match server rule, got {boss.BossReward}");
    }

    private static void BuildsProgressNodesForBossCycle()
    {
        var nodes = EquipmentPresenter.BuildProgressNodes(7);
        if (nodes.Count != 5) throw new Exception($"progress should show five nodes, got {nodes.Count}");
        AssertEqual("6", nodes[0].Label, "cycle should start at floor 6");
        AssertEqual("B10", nodes[4].Label, "cycle should end at boss floor 10");
        if (!nodes[0].Passed) throw new Exception("previous floor in cycle should be passed");
        if (!nodes[1].Current) throw new Exception("current floor should be marked");
        if (!nodes[4].IsBoss) throw new Exception("fifth node should be boss");
    }

    private static void BuildsCraftAndReincarnationPlans()
    {
        var selected = Equipment("selected", 0, 2, 2, Affix("strength", 2, 30), Affix("crit_rate", 2, 0.08f));
        var current = Equipment("current", 0, 2, 6, Affix("strength", 2, 10));
        var craft = EquipmentPresenter.BuildCraftPlan(selected, current, 12, 3, 45f);
        AssertContains(craft.TransferLine, "继承强化", "craft plan should show inheritance when current upgrade is higher");
        AssertContains(craft.UpgradeLine, "基础材料 12/10", "upgrade line should show next level cost");
        AssertContains(craft.ReforgeLine, "共 2 份", "reforge line should count affixes");
        AssertContains(craft.ComposeLine, "合成可用", "compose line should show affordability");
        AssertContains(craft.CleanupLine, "弱装 3 件", "cleanup line should show weak items");

        var maxed = Equipment("maxed", 0, 2, 10, Affix("strength", 2, 30));
        var maxedCraft = EquipmentPresenter.BuildCraftPlan(maxed, null, 9, 0, 0f);
        AssertContains(maxedCraft.UpgradeLine, "已强化到 +10", "max upgrade should not suggest another level");
        AssertContains(maxedCraft.ComposeLine, "还差 1 个基础材料", "compose line should show exact missing materials");

        var reincarnReady = EquipmentPresenter.BuildReincarnationPlan(15, 1, 12, true, "伤害");
        AssertContains(reincarnReady.StatusLine, "可转生", "ready reincarnation should be called out");
        AssertContains(reincarnReady.RewardLine, "+3", "reincarnation reward should use floor/5");
        AssertContains(reincarnReady.ResetLine, "重置层数", "reset consequences should be explicit");
        AssertContains(reincarnReady.NextTalentLine, "伤害", "next talent recommendation should be shown");

        var locked = EquipmentPresenter.BuildReincarnationPlan(6, 0, 6, false, "");
        AssertContains(locked.RewardLine, "继续推进 4 层", "locked reincarnation should show remaining floors");
        AssertContains(locked.NextTalentLine, "暂无推荐", "empty talent recommendation should be explicit");
    }

    private static void BuildsBattleStageStateForVisualCombat()
    {
        var winning = EquipmentPresenter.BuildBattleStageState(2, 40f);
        AssertContains(winning.HeroPower, "战力", "hero power should be display-ready");
        AssertContains(winning.MonsterName, "骸骨守卫", "normal stage should name monster");
        AssertContains(winning.Status, "压制", "strong hero should show pressure status");
        AssertNear(1f, winning.HeroHealth, 0.001f, "winning hero bar should be full");
        if (winning.MonsterHealth >= 1f) throw new Exception($"winning monster bar should be damaged, got {winning.MonsterHealth}");

        var boss = EquipmentPresenter.BuildBattleStageState(5, 20f);
        AssertContains(boss.MonsterName, "墓道巨像", "boss stage should name boss");
        AssertContains(boss.Status, "受阻", "weak boss fight should show blocked status");
        if (!boss.IsBoss) throw new Exception("boss stage should set IsBoss");
    }

    private static void BuildsCombatBeatStateForHitFeedback()
    {
        var idle = EquipmentPresenter.BuildCombatBeatState(2, 40f, 1f, 0.5f);
        var hit = EquipmentPresenter.BuildCombatBeatState(2, 40f, 0.25f, 0.5f);

        if (!hit.Active) throw new Exception("mid-beat state should be active");
        AssertContains(hit.DamageText, "-", "hit state should show damage text");
        if (hit.MonsterHealth >= idle.MonsterHealth)
            throw new Exception($"hit should visibly reduce monster health during the beat, got hit={hit.MonsterHealth}, idle={idle.MonsterHealth}");
        if (hit.HeroOffset <= 0f || hit.MonsterOffset <= 0f)
            throw new Exception($"hit should move both combatants, hero={hit.HeroOffset}, monster={hit.MonsterOffset}");
        if (idle.Active) throw new Exception("elapsed beat should become inactive");

        var boss = EquipmentPresenter.BuildCombatBeatState(5, 100f, 0.2f, 0.5f);
        AssertContains(boss.DamageText, "重击", "boss hit should have stronger feedback copy");
    }

    private static void NamesDungeonZonesAndMonsters()
    {
        var firstZone = EquipmentPresenter.BuildDungeonState(1, 10f);
        AssertContains(firstZone.Title, "荒石墓道", "early floors should show first zone");
        AssertContains(firstZone.Monster, "骸骨守卫", "normal encounter should use named monster");

        var secondZone = EquipmentPresenter.BuildDungeonState(6, 100f);
        AssertContains(secondZone.Title, "烛火地窖", "floor 6 should move to second zone");
        AssertContains(secondZone.Monster, "地窖盗匪", "second zone should use matching monster");

        var boss = EquipmentPresenter.BuildDungeonState(10, 100f);
        AssertContains(boss.Monster, "守层 Boss：烛焰看守", "boss encounter should use named boss");
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

    private static void AssertContainsComparison(System.Collections.Generic.IList<EquipmentComparisonRow> rows, string label, bool positive, string message)
    {
        foreach (var row in rows)
        {
            if (row.Label != label) continue;
            if (positive && row.Delta <= 0f) throw new Exception($"{message}: {label} delta should be positive, got {row.Delta}");
            if (!positive && row.Delta >= 0f) throw new Exception($"{message}: {label} delta should be negative, got {row.Delta}");
            return;
        }
        throw new Exception($"{message}: missing {label}");
    }
}
#endif
