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
        public float MonsterPower;
        public float GateProgress;
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
            if (eq == null) return "Select an equipment item to inspect stats and actions.";
            float score = Score(eq);
            float delta = score - Score(current);
            string text = $"{eq.name} +{eq.upgrade}\n";
            text += $"Slot: {SlotName(eq.slot)}\n";
            text += $"Rarity: {RarityName(eq.rarity)}\n";
            text += $"Score: {score:F0}\n";
            if (current != null) text += $"Delta: {delta:+0;-0;0}\n";
            text += "\n";

            if (eq.affixes == null || eq.affixes.Length == 0)
            {
                text += "No affixes.";
            }
            else
            {
                text += "Affixes:\n";
                foreach (var affix in eq.affixes)
                {
                    text += $"- {FormatAffix(affix)}\n";
                }
            }
            return text;
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

            return new DungeonState
            {
                Title = boss ? $"Floor {floor} Boss Gate" : $"Floor {floor} Dungeon Run",
                Monster = boss ? $"Boss Warden\nPower {monsterPower:F1}" : $"Dungeon Enemy\nPower {monsterPower:F1}",
                Battle = ratio >= 1f
                    ? $"Advantage {ratio:F1}x. Next battle should clear."
                    : $"Underpowered {ratio:F1}x. Upgrade or equip stronger loot.",
                MonsterPower = monsterPower,
                GateProgress = Clamp01(gateProgress / 5f),
                IsBoss = boss,
            };
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
                case "strength": return "Strength";
                case "max_hp": return "Max Health";
                case "armor": return "Armor";
                case "fire_dmg": return "Fire Damage";
                case "cold_dmg": return "Cold Damage";
                case "lightning_dmg": return "Lightning Damage";
                case "crit_rate": return "Critical Chance";
                case "crit_damage": return "Critical Damage";
                case "attack_speed": return "Attack Speed";
                case "agility": return "Agility";
                case "intellect": return "Intellect";
                case "vitality": return "Vitality";
                default: return type;
            }
        }

        private static string RarityName(int rarity)
        {
            switch (rarity)
            {
                case 0: return "Common";
                case 1: return "Magic";
                case 2: return "Rare";
                case 3: return "Legendary";
                case 4: return "Artifact";
                default: return "?";
            }
        }

        private static string SlotName(int slot)
        {
            switch (slot)
            {
                case 0: return "Weapon";
                case 1: return "Helmet";
                case 2: return "Armor";
                case 3: return "Gloves";
                case 4: return "Boots";
                case 5: return "Ring 1";
                case 6: return "Ring 2";
                case 7: return "Neck";
                default: return "?";
            }
        }
    }
}
