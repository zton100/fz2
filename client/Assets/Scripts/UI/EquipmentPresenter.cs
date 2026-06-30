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

    public struct DungeonState
    {
        public string Title;
        public string Monster;
        public string Battle;
        public string BossHint;
        public float MonsterPower;
        public float GateProgress;
        public bool IsBoss;
        public int BossReward;
    }

    public struct BattleStageState
    {
        public string HeroName;
        public string HeroPower;
        public string MonsterName;
        public string MonsterPower;
        public string Status;
        public float HeroHealth;
        public float MonsterHealth;
        public bool IsBoss;
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

        public static List<EquipmentAction> DetailActions(EquipmentDTO eq, bool isEquipped)
        {
            var actions = new List<EquipmentAction>();
            if (eq == null) return actions;

            if (isEquipped)
            {
                actions.Add(new EquipmentAction("unequip", L10n.UIUnequip));
                return actions;
            }

            actions.Add(new EquipmentAction("equip", L10n.UIEquip));
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

            return new DungeonState
            {
                Title = boss ? $"第 {floor} 层 Boss 关" : $"第 {floor} 层地下城",
                Monster = boss ? $"守层 Boss\n战力 {monsterPower:F1}" : $"地下城怪物\n战力 {monsterPower:F1}",
                Battle = ratio >= 1f
                    ? $"优势 {ratio:F1}x，下一场战斗大概率通过。"
                    : $"战力不足 {ratio:F1}x，请强化或穿戴更强装备。",
                BossHint = bossHint,
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
                MonsterName = dungeon.IsBoss ? "守层 Boss" : "地下城怪物",
                MonsterPower = $"战力 {dungeon.MonsterPower:F1}",
                Status = status,
                HeroHealth = heroHealth,
                MonsterHealth = monsterHealth,
                IsBoss = dungeon.IsBoss,
            };
        }

        public static int BossRewardAtFloor(int floor)
        {
            return floor > 0 && floor % 5 == 0 ? floor * 2 : 0;
        }

        public static float MonsterPowerAtFloor(int floor)
        {
            if (floor <= 0) return 3f;
            float normal;
            if (floor <= 20)
            {
                normal = 3f + (floor - 1) * 5f;
            }
            else
            {
                normal = 98f * (float)Math.Pow(1.05f, floor - 20);
            }
            return floor % 5 == 0 ? normal * 1.8f : normal;
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

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
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
