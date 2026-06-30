using EquipmentIdle.Net;
using System.Collections.Generic;

namespace EquipmentIdle.UI
{
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

        private static EquipmentDTO CurrentForSlot(Dictionary<int, EquipmentDTO> currentBySlot, int slot)
        {
            return currentBySlot.TryGetValue(slot, out var eq) ? eq : null;
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
