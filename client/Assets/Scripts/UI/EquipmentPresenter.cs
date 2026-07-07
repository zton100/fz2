using EquipmentIdle.Data;
using EquipmentIdle.Net;
using System;
using System.Collections.Generic;

namespace EquipmentIdle.UI
{
    public struct EquipmentAction
    {
        public string Id;
        public string Label;

        public EquipmentAction(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    public enum EquipmentBagFilter
    {
        All,
        Upgrades,
        Rare,
        Decompose,
    }

    public struct DungeonState
    {
        public string Title;
        public string Monster;
        public string Battle;
        public string BossHint;
        public string Zone;
        public float MonsterPower;
        public float GateProgress;
        public bool IsBoss;
        public int BossReward;
    }

    public struct BattleStageState
    {
        public string HeroName;
        public string HeroPower;
        public string Zone;
        public string MonsterName;
        public string MonsterPower;
        public string Status;
        public float HeroHealth;
        public float MonsterHealth;
        public bool IsBoss;
    }

    public struct CombatBeatState
    {
        public string DamageText;
        public float MonsterHealth;
        public float HeroOffset;
        public float MonsterOffset;
        public float ImpactOpacity;
        public bool Active;
    }

    public struct ProgressNodeState
    {
        public string Label;
        public bool Passed;
        public bool Current;
        public bool IsBoss;

        public ProgressNodeState(string label, bool passed, bool current, bool isBoss)
        {
            Label = label;
            Passed = passed;
            Current = current;
            IsBoss = isBoss;
        }
    }

    public struct CraftPlanState
    {
        public string TransferLine;
        public string UpgradeLine;
        public string ReforgeLine;
        public string ComposeLine;
        public string CleanupLine;
    }

    public struct ReincarnationPlanState
    {
        public string StatusLine;
        public string RewardLine;
        public string ResetLine;
        public string NextTalentLine;
    }

    public struct EquipmentComparisonRow
    {
        public string Label;
        public string SelectedValue;
        public string CurrentValue;
        public string DeltaText;
        public float Delta;

        public EquipmentComparisonRow(string label, string selectedValue, string currentValue, string deltaText, float delta)
        {
            Label = label;
            SelectedValue = selectedValue;
            CurrentValue = currentValue;
            DeltaText = deltaText;
            Delta = delta;
        }
    }

    public static class EquipmentPresenter
    {
        public static List<EquipmentDTO> SortBagForDisplay(IList<EquipmentDTO> bag, IList<EquipmentDTO> equipped)
        {
            var currentBySlot = new Dictionary<int, EquipmentDTO>();
            if (equipped != null)
            {
                foreach (var eq in equipped)
                {
                    if (eq != null) currentBySlot[eq.slot] = eq;
                }
            }

            var list = new List<EquipmentDTO>();
            if (bag != null) list.AddRange(bag);
            list.Sort((a, b) =>
            {
                float aDelta = Score(a) - Score(CurrentForSlot(currentBySlot, a.slot));
                float bDelta = Score(b) - Score(CurrentForSlot(currentBySlot, b.slot));
                int byUpgrade = bDelta.CompareTo(aDelta);
                if (byUpgrade != 0) return byUpgrade;
                int byRarity = b.rarity.CompareTo(a.rarity);
                if (byRarity != 0) return byRarity;
                return Score(b).CompareTo(Score(a));
            });
            return list;
        }

        public static bool ShouldShowInBag(EquipmentDTO eq, EquipmentDTO current, EquipmentBagFilter filter, bool locked)
        {
            if (eq == null) return false;
            float delta = Score(eq) - Score(current);
            switch (filter)
            {
                case EquipmentBagFilter.Upgrades:
                    return delta > 0f;
                case EquipmentBagFilter.Rare:
                    return eq.rarity >= 2;
                case EquipmentBagFilter.Decompose:
                    return !locked && eq.rarity <= 1 && delta <= 0f;
                default:
                    return true;
            }
        }

        public static float Score(EquipmentDTO eq)
        {
            if (eq == null) return 0f;
            float score = (eq.rarity + 1) * 100f + eq.upgrade * 20f;
            if (eq.affixes == null) return score;
            foreach (var affix in eq.affixes)
            {
                if (affix == null) continue;
                score += affix.value + affix.tier * 10f;
            }
            return score;
        }

        public static string FormatAffix(AffixData affix)
        {
            if (affix == null) return "";
            string name = AffixName(affix.type);
            if (affix.type == "crit_rate" || affix.type == "attack_speed")
            {
                return $"{name} +{affix.value * 100f:F1}%";
            }
            return $"{name} +{affix.value:F0}";
        }

        public static string BuildDetail(EquipmentDTO eq, EquipmentDTO current)
        {
            if (eq == null) return "选择一件装备查看属性和操作。";
            float score = Score(eq);
            float delta = score - Score(current);
            string text = $"{eq.name} +{eq.upgrade}\n";
            text += $"部位：{SlotName(eq.slot)}\n";
            text += $"品质：{RarityName(eq.rarity)}\n";
            text += $"评分：{score:F0}\n";
            if (current != null) text += $"对比：{delta:+0;-0;0}\n";
            text += "\n";

            if (eq.affixes == null || eq.affixes.Length == 0)
            {
                text += "无词缀。";
            }
            else
            {
                text += "词缀：\n";
                foreach (var affix in eq.affixes)
                {
                    text += $"- {FormatAffix(affix)}\n";
                }
            }
            return text;
        }

        public static string BuildSelectedSummary(EquipmentDTO eq)
        {
            if (eq == null) return "选择一件装备查看属性和操作。";
            return $"{eq.name} +{eq.upgrade}\n"
                + $"{SlotName(eq.slot)} · {RarityName(eq.rarity)}\n"
                + $"评分 {Score(eq):F0}\n"
                + BuildAffixSummary(eq);
        }

        public static string BuildCurrentSummary(EquipmentDTO current)
        {
            if (current == null) return "当前部位未穿戴装备。\n穿戴后会直接补齐新部位。";
            return $"{current.name} +{current.upgrade}\n"
                + $"{SlotName(current.slot)} · {RarityName(current.rarity)}\n"
                + $"评分 {Score(current):F0}\n"
                + BuildAffixSummary(current);
        }

        public static List<EquipmentComparisonRow> BuildComparisonRows(EquipmentDTO selected, EquipmentDTO current)
        {
            var rows = new List<EquipmentComparisonRow>();
            if (selected == null) return rows;

            float selectedScore = Score(selected);
            float currentScore = Score(current);
            rows.Add(new EquipmentComparisonRow("评分", $"{selectedScore:F0}", current != null ? $"{currentScore:F0}" : "-", FormatSigned(selectedScore - currentScore), selectedScore - currentScore));
            rows.Add(new EquipmentComparisonRow("强化", $"+{selected.upgrade}", current != null ? $"+{current.upgrade}" : "-", FormatSigned(selected.upgrade - (current != null ? current.upgrade : 0)), selected.upgrade - (current != null ? current.upgrade : 0)));
            rows.Add(new EquipmentComparisonRow("品质", RarityName(selected.rarity), current != null ? RarityName(current.rarity) : "-", FormatSigned(selected.rarity - (current != null ? current.rarity : -1)), selected.rarity - (current != null ? current.rarity : -1)));

            var keys = new List<string>();
            var selectedAffixes = AffixTotals(selected, keys);
            var currentAffixes = AffixTotals(current, keys);
            foreach (var key in keys)
            {
                float selectedValue = selectedAffixes.ContainsKey(key) ? selectedAffixes[key] : 0f;
                float currentValue = currentAffixes.ContainsKey(key) ? currentAffixes[key] : 0f;
                float delta = selectedValue - currentValue;
                rows.Add(new EquipmentComparisonRow(AffixName(key), FormatComparableValue(key, selectedValue), current != null ? FormatComparableValue(key, currentValue) : "-", FormatSignedValue(key, delta), delta));
            }
            return rows;
        }

        public static string BuildLootLine(EquipmentDTO eq)
        {
            if (eq == null) return "";
            string prefix = LootPrefix(eq.rarity);
            return $"{prefix} [{RarityName(eq.rarity)}] {eq.name} +{eq.upgrade}";
        }

        public static string BuildLootToast(EquipmentDTO eq)
        {
            if (eq == null) return "";
            string prefix = LootPrefix(eq.rarity);
            return $"{prefix}：{RarityName(eq.rarity)} {eq.name}";
        }

        public static string BuildLootCeremonyText(EquipmentDTO eq, bool isUpgrade)
        {
            if (eq == null) return "";
            if (eq.rarity >= 3) return $"{RarityName(eq.rarity)} 掉落：{eq.name}";
            if (eq.rarity >= 2) return $"稀有掉落：{eq.name}";
            if (isUpgrade) return $"可穿戴提升：{eq.name}";
            return "";
        }

        public static string BuildBossClearBanner(int clearedFloor)
        {
            if (clearedFloor <= 0 || clearedFloor % 5 != 0) return "";
            return $"Boss 击破：第 {clearedFloor} 层 · 基础材料 +{BossRewardAtFloor(clearedFloor)}";
        }

        public static List<ProgressNodeState> BuildProgressNodes(int floor)
        {
            if (floor < 1) floor = 1;
            int start = floor - ((floor - 1) % 5);
            var nodes = new List<ProgressNodeState>();
            for (int i = 0; i < 5; i++)
            {
                int nodeFloor = start + i;
                nodes.Add(new ProgressNodeState(
                    nodeFloor % 5 == 0 ? $"B{nodeFloor}" : nodeFloor.ToString(),
                    nodeFloor < floor,
                    nodeFloor == floor,
                    nodeFloor % 5 == 0));
            }
            return nodes;
        }

        public static string BuildEquipmentLine(EquipmentDTO eq, EquipmentDTO current, bool isEquipped)
        {
            if (eq == null) return "";
            string text = $"[{RarityName(eq.rarity)}] {eq.name} +{eq.upgrade}";
            if (eq.affixes != null && eq.affixes.Length > 0)
            {
                text += $"  {FormatAffix(eq.affixes[0])}";
            }

            if (isEquipped)
            {
                return text + "  已穿戴";
            }

            if (current == null)
            {
                return text + "  新部位";
            }

            float delta = Score(eq) - Score(current);
            if (delta > 0f)
            {
                return text + $"  提升 +{delta:F0}";
            }
            if (delta < 0f)
            {
                return text + $"  更弱 {delta:F0}";
            }
            return text + "  持平";
        }

        public static List<EquipmentAction> DetailActions(EquipmentDTO eq, bool isEquipped, EquipmentDTO current)
        {
            var actions = new List<EquipmentAction>();
            if (eq == null) return actions;

            if (isEquipped)
            {
                actions.Add(new EquipmentAction("unequip", L10n.UIUnequip));
                return actions;
            }

            actions.Add(new EquipmentAction("equip", L10n.UIEquip));
            if (CanInheritUpgrade(current, eq))
            {
                actions.Add(new EquipmentAction("transfer_upgrade", "继承强化"));
            }
            actions.Add(new EquipmentAction("upgrade", L10n.UIUpgrade));
            actions.Add(new EquipmentAction("reforge", L10n.UIReforge));
            actions.Add(new EquipmentAction("decompose", L10n.UIDecompose));
            return actions;
        }

        public static DungeonState BuildDungeonState(int floor, float playerPower)
        {
            if (floor < 1) floor = 1;
            bool boss = floor % 5 == 0;
            float monsterPower = MonsterPowerAtFloor(floor);
            float ratio = monsterPower <= 0f ? 0f : playerPower / monsterPower;
            int gateProgress = ((floor - 1) % 5) + 1;
            int nextBoss = boss ? floor : floor + (5 - gateProgress);
            int bossReward = BossRewardAtFloor(nextBoss);
            string bossHint = boss
                ? $"首通奖励：基础材料 +{bossReward}"
                : $"距 Boss 关还差 {5 - gateProgress} 层，首通奖励基础材料 +{bossReward}";
            string zone = ZoneName(floor);
            string monsterName = MonsterName(floor, boss);

            return new DungeonState
            {
                Title = boss ? $"第 {floor} 层 Boss 关 · {zone}" : $"第 {floor} 层 · {zone}",
                Monster = boss ? $"守层 Boss：{monsterName}\n战力 {monsterPower:F1}" : $"{monsterName}\n战力 {monsterPower:F1}",
                Battle = ratio >= 1f
                    ? $"优势 {ratio:F1}x，下一场战斗大概率通过。"
                    : $"战力不足 {ratio:F1}x，请强化或穿戴更强装备。",
                BossHint = bossHint,
                Zone = zone,
                MonsterPower = monsterPower,
                GateProgress = Clamp01(gateProgress / 5f),
                IsBoss = boss,
                BossReward = bossReward,
            };
        }

        public static BattleStageState BuildBattleStageState(int floor, float playerPower)
        {
            var dungeon = BuildDungeonState(floor, playerPower);
            float ratio = dungeon.MonsterPower <= 0f ? 0f : playerPower / dungeon.MonsterPower;
            float heroHealth = ratio >= 1f ? 1f : Clamp01(ratio);
            float monsterHealth = ratio >= 1f ? Clamp01(1f / ratio) : 1f;
            string status;
            if (ratio >= 1.5f)
            {
                status = "压制";
            }
            else if (ratio >= 1f)
            {
                status = "优势";
            }
            else if (ratio >= 0.75f)
            {
                status = "僵持";
            }
            else
            {
                status = "受阻";
            }

            return new BattleStageState
            {
                HeroName = "冒险者",
                HeroPower = $"战力 {playerPower:F1}",
                Zone = dungeon.Zone,
                MonsterName = MonsterName(floor, dungeon.IsBoss),
                MonsterPower = $"战力 {dungeon.MonsterPower:F1}",
                Status = status,
                HeroHealth = heroHealth,
                MonsterHealth = monsterHealth,
                IsBoss = dungeon.IsBoss,
            };
        }

        public static CombatBeatState BuildCombatBeatState(int floor, float playerPower, float elapsedSeconds, float durationSeconds)
        {
            var stage = BuildBattleStageState(floor, playerPower);
            if (playerPower <= 0f || elapsedSeconds < 0f || durationSeconds <= 0f)
            {
                return new CombatBeatState { MonsterHealth = stage.MonsterHealth };
            }

            float progress = Clamp01(elapsedSeconds / durationSeconds);
            bool active = progress < 1f;
            float pulse = active ? (float)Math.Sin(progress * Math.PI) : 0f;
            float monsterPower = MonsterPowerAtFloor(floor);
            float ratio = monsterPower <= 0f ? 0f : playerPower / monsterPower;
            float damagePressure = ratio >= 1f ? 0.46f : 0.18f * Clamp01(ratio);
            float monsterHealth = Clamp01(stage.MonsterHealth - pulse * damagePressure);
            int damage = Math.Max(1, (int)Math.Round(playerPower * (stage.IsBoss ? 0.58f : 0.76f)));

            return new CombatBeatState
            {
                DamageText = stage.IsBoss ? $"重击 -{damage}" : $"-{damage}",
                MonsterHealth = monsterHealth,
                HeroOffset = pulse * 18f,
                MonsterOffset = pulse * 12f,
                ImpactOpacity = active ? 1f - progress : 0f,
                Active = active,
            };
        }

        public static int BossRewardAtFloor(int floor)
        {
            return floor > 0 && floor % 5 == 0 ? floor * 3 : 0;
        }

        public static float MonsterPowerAtFloor(int floor)
        {
            if (floor <= 0) return 3f;
            float normal;
            if (floor <= 20)
            {
                normal = 3f + (floor - 1) * 5f;
            }
            else if (floor <= 80)
            {
                normal = 98f * (float)Math.Pow(1.05f, floor - 20);
            }
            else if (floor <= 120)
            {
                float baseAt80 = 98f * (float)Math.Pow(1.05f, 60);
                normal = baseAt80 * (float)Math.Pow(1.055f, floor - 80);
            }
            else
            {
                float baseAt80 = 98f * (float)Math.Pow(1.05f, 60);
                float baseAt120 = baseAt80 * (float)Math.Pow(1.055f, 40);
                normal = baseAt120 * (float)Math.Pow(1.025f, floor - 120);
            }
            return floor % 5 == 0 ? normal * 1.2f : normal;
        }

        public static string BuildNextGoal(int floor, float playerPower, bool canReincarn, int bagCount, int baseMaterials)
        {
            if (canReincarn)
            {
                return "目标：进行转生，领取魂点并提升永久天赋。";
            }

            float monsterPower = MonsterPowerAtFloor(floor);
            if (playerPower <= 0f)
            {
                return "目标：连接并同步角色，开始自动战斗。";
            }

            if (playerPower < monsterPower)
            {
                if (bagCount > 0) return "目标：穿戴更强掉落，或强化核心装备突破本层。";
                if (baseMaterials >= 10) return "目标：先合成新装备，再穿戴提升装备突破本层。";
                return "目标：等待掉落，或分解弱装积累强化材料。";
            }

            int nextBoss = floor + (5 - ((floor - 1) % 5));
            return $"目标：自动战斗中，向第 {nextBoss} 层 Boss 关推进。";
        }

        public static string BuildTalentLine(string name, int level, int maxLevel, string description, int souls)
        {
            string state;
            if (level >= maxLevel)
            {
                state = "满级";
            }
            else if (souls > 0)
            {
                state = "可升级";
            }
            else
            {
                state = "需要魂点";
            }
            return $"{name} Lv{level}/{maxLevel} - {description}  {state}";
        }

        public static CraftPlanState BuildCraftPlan(EquipmentDTO selected, EquipmentDTO current, int baseMaterials, int weakCount, float equipBestDelta)
        {
            string transferLine = "选择同部位新装备后可继承旧装强化。";
            string upgradeLine = "选择背包装备后显示强化消耗。";
            string reforgeLine = "选择带词缀装备后显示重铸材料。";
            if (selected != null)
            {
                transferLine = BuildTransferLine(current, selected);
                int nextLevel = selected.upgrade + 1;
                if (selected.upgrade >= 10)
                {
                    upgradeLine = $"{selected.name} 已强化到 +10。";
                }
                else
                {
                    int cost = UpgradeCost(nextLevel);
                    upgradeLine = $"强化 {selected.name} +{nextLevel}：基础材料 {baseMaterials}/{cost}";
                }

                int reforgeCost = selected.affixes == null ? 0 : selected.affixes.Length;
                reforgeLine = reforgeCost > 0
                    ? $"重铸 {selected.name}：按词缀 Tier 消耗材料，共 {reforgeCost} 份。"
                    : $"{selected.name} 无词缀，重铸收益低。";
            }

            string composeLine = baseMaterials >= 10
                ? $"合成可用：基础材料 {baseMaterials}/10，可补缺失部位。"
                : $"合成还差 {10 - baseMaterials} 个基础材料。";
            string cleanupLine = weakCount > 0
                ? $"可清理弱装 {weakCount} 件；分解强化装会返还部分基础材料。"
                : equipBestDelta > 0f
                    ? $"一键穿戴预计评分 +{equipBestDelta:F0}。"
                    : "当前没有明显弱装；替换旧强化装后可分解回收部分材料。";

            return new CraftPlanState
            {
                TransferLine = transferLine,
                UpgradeLine = upgradeLine,
                ReforgeLine = reforgeLine,
                ComposeLine = composeLine,
                CleanupLine = cleanupLine,
            };
        }

        public static bool CanInheritUpgrade(EquipmentDTO source, EquipmentDTO target)
        {
            return source != null
                && target != null
                && source.uid != target.uid
                && source.slot == target.slot
                && source.upgrade > target.upgrade;
        }

        public static string BuildTransferLine(EquipmentDTO source, EquipmentDTO target)
        {
            if (target == null) return "选择同部位新装备后可继承旧装强化。";
            if (source == null) return $"{target.name} 当前部位无旧装可继承。";
            if (source.slot != target.slot) return "只能继承同部位装备强化。";
            if (!CanInheritUpgrade(source, target))
            {
                return $"{target.name} 暂无更高强化可继承。";
            }
            return $"继承强化：{source.name} +{source.upgrade} -> {target.name}，旧装回到 +{target.upgrade}。";
        }

        public static ReincarnationPlanState BuildReincarnationPlan(int floor, int souls, int maxFloor, bool canReincarn, string nextTalentName)
        {
            int earned = floor >= 10 ? floor / 5 : 0;
            string status = canReincarn
                ? $"可转生：当前第 {floor} 层。"
                : $"转生条件：第 10 层，当前第 {floor} 层。";
            string reward = canReincarn
                ? $"本次转生预计获得魂点 +{earned}，当前魂点 {souls}。"
                : $"继续推进 {Math.Max(0, 10 - floor)} 层后开启首次转生。";
            string reset = "转生会重置层数、背包、装备和材料，保留历史最高层与天赋。";
            string nextTalent = string.IsNullOrEmpty(nextTalentName)
                ? "所有核心天赋已满或暂无推荐。"
                : $"推荐下一个魂点投入：{nextTalentName}。";

            return new ReincarnationPlanState
            {
                StatusLine = status,
                RewardLine = reward,
                ResetLine = $"历史最高层：{maxFloor}。{reset}",
                NextTalentLine = nextTalent,
            };
        }

        public static List<EquipmentDTO> BulkDecomposeCandidates(IList<EquipmentDTO> bag, IList<EquipmentDTO> equipped)
        {
            var currentBySlot = new Dictionary<int, EquipmentDTO>();
            if (equipped != null)
            {
                foreach (var eq in equipped)
                {
                    if (eq != null) currentBySlot[eq.slot] = eq;
                }
            }

            var candidates = new List<EquipmentDTO>();
            if (bag == null) return candidates;
            foreach (var eq in bag)
            {
                if (eq == null || eq.rarity > 1) continue;
                float delta = Score(eq) - Score(CurrentForSlot(currentBySlot, eq.slot));
                if (delta <= 0f) candidates.Add(eq);
            }
            return candidates;
        }

        public static float EquipBestDelta(IList<EquipmentDTO> bag, IList<EquipmentDTO> equipped)
        {
            var currentBySlot = new Dictionary<int, EquipmentDTO>();
            if (equipped != null)
            {
                foreach (var eq in equipped)
                {
                    if (eq != null) currentBySlot[eq.slot] = eq;
                }
            }

            float delta = 0f;
            if (bag == null) return delta;
            foreach (var eq in bag)
            {
                if (eq == null) continue;
                var current = CurrentForSlot(currentBySlot, eq.slot);
                float gain = Score(eq) - Score(current);
                if (gain <= 0f) continue;
                currentBySlot[eq.slot] = eq;
            }

            if (equipped != null)
            {
                foreach (var eq in equipped)
                {
                    if (eq != null) delta -= Score(eq);
                }
            }
            foreach (var eq in currentBySlot.Values)
            {
                delta += Score(eq);
            }
            return delta;
        }

        private static EquipmentDTO CurrentForSlot(Dictionary<int, EquipmentDTO> currentBySlot, int slot)
        {
            return currentBySlot.TryGetValue(slot, out var eq) ? eq : null;
        }

        private static string BuildAffixSummary(EquipmentDTO eq)
        {
            if (eq == null || eq.affixes == null || eq.affixes.Length == 0) return "无词缀。";
            string text = "";
            foreach (var affix in eq.affixes)
            {
                if (affix == null) continue;
                text += FormatAffix(affix) + "\n";
            }
            return text.TrimEnd('\n');
        }

        private static Dictionary<string, float> AffixTotals(EquipmentDTO eq, List<string> keys)
        {
            var totals = new Dictionary<string, float>();
            if (eq == null || eq.affixes == null) return totals;
            foreach (var affix in eq.affixes)
            {
                if (affix == null || string.IsNullOrEmpty(affix.type)) continue;
                if (!totals.ContainsKey(affix.type))
                {
                    totals[affix.type] = 0f;
                    if (!keys.Contains(affix.type)) keys.Add(affix.type);
                }
                totals[affix.type] += affix.value;
            }
            return totals;
        }

        private static string FormatSigned(float delta)
        {
            if (delta > 0f) return $"↑ +{delta:F0}";
            if (delta < 0f) return $"↓ {delta:F0}";
            return "持平";
        }

        private static string FormatSignedValue(string type, float delta)
        {
            if (Math.Abs(delta) < 0.0001f) return "持平";
            string prefix = delta > 0f ? "↑ +" : "↓ ";
            float value = delta;
            if (type == "crit_rate" || type == "attack_speed")
            {
                return $"{prefix}{value * 100f:F1}%";
            }
            return $"{prefix}{value:F0}";
        }

        private static string FormatComparableValue(string type, float value)
        {
            if (type == "crit_rate" || type == "attack_speed")
            {
                return $"{value * 100f:F1}%";
            }
            return $"{value:F0}";
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static int UpgradeCost(int targetLevel)
        {
            int[] costs = { 0, 3, 5, 10, 18, 30, 50, 80, 120, 180, 250 };
            if (targetLevel < 0) return 0;
            if (targetLevel >= costs.Length) return costs[costs.Length - 1];
            return costs[targetLevel];
        }

        private static string AffixName(string type)
        {
            switch (type)
            {
                case "strength": return "力量";
                case "max_hp": return "最大生命";
                case "armor": return "护甲";
                case "fire_dmg": return "火焰伤害";
                case "cold_dmg": return "冰霜伤害";
                case "lightning_dmg": return "闪电伤害";
                case "crit_rate": return "暴击率";
                case "crit_damage": return "暴击伤害";
                case "attack_speed": return "攻击速度";
                case "agility": return "敏捷";
                case "intellect": return "智力";
                case "vitality": return "体质";
                default: return type;
            }
        }

        private static string RarityName(int rarity)
        {
            switch (rarity)
            {
                case 0: return "普通";
                case 1: return "魔法";
                case 2: return "稀有";
                case 3: return "传奇";
                case 4: return "神器";
                default: return "?";
            }
        }

        private static string LootPrefix(int rarity)
        {
            if (rarity >= 4) return "神器掉落";
            if (rarity == 3) return "传奇掉落";
            if (rarity == 2) return "稀有掉落";
            return "掉落";
        }

        private static string ZoneName(int floor)
        {
            string[] zones =
            {
                "荒石墓道",
                "烛火地窖",
                "寒铁监牢",
                "龙骨回廊",
                "深渊祭坛",
            };
            int index = Math.Max(0, (floor - 1) / 5) % zones.Length;
            return zones[index];
        }

        private static string MonsterName(int floor, bool boss)
        {
            string[] bosses =
            {
                "墓道巨像",
                "烛焰看守",
                "寒铁典狱长",
                "龙骨骑士",
                "深渊司祭",
            };
            string[] monsters =
            {
                "骸骨守卫",
                "地窖盗匪",
                "铁链囚徒",
                "骨翼猎手",
                "深渊信徒",
            };
            int index = Math.Max(0, (floor - 1) / 5) % monsters.Length;
            return boss ? bosses[index] : monsters[index];
        }

        private static string SlotName(int slot)
        {
            switch (slot)
            {
                case 0: return "武器";
                case 1: return "头盔";
                case 2: return "护甲";
                case 3: return "手套";
                case 4: return "靴子";
                case 5: return "戒指1";
                case 6: return "戒指2";
                case 7: return "项链";
                default: return "?";
            }
        }
    }
}
