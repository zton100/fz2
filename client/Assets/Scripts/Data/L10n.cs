using System.Collections.Generic;
using UnityEngine;

namespace EquipmentIdle.Data
{
    /// <summary>
    /// L10n provides user-facing text strings with built-in Chinese defaults.
    /// Call L10n.SetOverrides(json) to replace with another language at runtime.
    /// </summary>
    public static class L10n
    {
        // ── Rarity ──
        public static string RarityCommon    { get; private set; } = "普通";
        public static string RarityMagic     { get; private set; } = "魔法";
        public static string RarityRare      { get; private set; } = "稀有";
        public static string RarityLegendary { get; private set; } = "传奇";
        public static string RarityArtifact  { get; private set; } = "神器";

        // ── Slots ──
        public static string SlotWeapon { get; private set; } = "武器";
        public static string SlotHelmet { get; private set; } = "头盔";
        public static string SlotArmor  { get; private set; } = "护甲";
        public static string SlotGloves { get; private set; } = "手套";
        public static string SlotBoots  { get; private set; } = "靴子";
        public static string SlotRing1  { get; private set; } = "戒指1";
        public static string SlotRing2  { get; private set; } = "戒指2";
        public static string SlotNeck   { get; private set; } = "项链";

        // ── UI ──
        public static string UIStatusDisconnected { get; private set; } = "disconnected";
        public static string UIStatusConnecting    { get; private set; } = "connecting...";
        public static string UIStatusConnected     { get; private set; } = "connected";
        public static string UINoSync              { get; private set; } = "(no sync yet)";
        public static string UIPower               { get; private set; } = "power: {0:F1}";
        public static string UIPowerDeltaUp        { get; private set; } = "power: {0:F1} <color=#4f4>+{1:F1}</color>";
        public static string UIPowerDeltaDown      { get; private set; } = "power: {0:F1} <color=#f44>{1:F1}</color>";
        public static string UIPowerLabel          { get; private set; } = "power: {0:F1}";
        public static string UIAccount             { get; private set; } = "Account:";
        public static string UIConnect             { get; private set; } = "Connect";
        public static string UIBackpack            { get; private set; } = "Backpack ({0} items):";
        public static string UIEquipBest           { get; private set; } = "Equip Best";
        public static string UIEquip               { get; private set; } = "Equip";
        public static string UIDecompose           { get; private set; } = "Dec";
        public static string UIReforge             { get; private set; } = "Ref";
        public static string UIUpgrade             { get; private set; } = "Up";
        public static string UIWorkshop            { get; private set; } = "--- Workshop ---";
        public static string UIMaterials           { get; private set; } = "Mats: ";
        public static string UIComposeLabel        { get; private set; } = "Compose (cost 10 base_mat):";
        public static string UIReincarnation       { get; private set; } = "--- Reincarnation ---";
        public static string UISouls               { get; private set; } = "Souls: {0}  MaxFloor: {1}  CanReincarn: {2}";
        public static string UIReincarnate         { get; private set; } = "REINCARNATE";
        public static string UITalentsLabel        { get; private set; } = "Talents (cost 1 soul each):";
        public static string UIStage               { get; private set; } = "EquipmentIdle - Stage 6";
        public static string UIStuckPrefix         { get; private set; } = "STUCK! Power {0:F0} < Monster {1:F0} at Floor {2}";
        public static string UIOfflineTitle        { get; private set; } = "--- Offline Summary ---";
        public static string UIOfflineDuration     { get; private set; } = "Duration: {0} (capped 8h)";
        public static string UIOfflineLoot         { get; private set; } = "Loot gained: {0} items";
        public static string UIOfflineFloors       { get; private set; } = "Floors advanced: {0}";
        public static string UIOfflineTicks        { get; private set; } = "Simulated ticks: {0}";
        public static string UIOK                  { get; private set; } = "OK";
        public static string UILootToast           { get; private set; } = "[Loot] {0} {1}";

        // ── Talent names & desc ──
        public static string TalentDamage      { get; private set; } = "damage";
        public static string TalentQuality      { get; private set; } = "quality";
        public static string TalentDrop         { get; private set; } = "drop";
        public static string TalentOfflineGain  { get; private set; } = "offline_gain";
        public static string TalentDamageDesc   { get; private set; } = "+5% dmg/lvl(max10)";
        public static string TalentQualityDesc  { get; private set; } = "+1 quality/lvl(max3)";
        public static string TalentDropDesc     { get; private set; } = "+3% drop/lvl(max10)";
        public static string TalentOfflineDesc  { get; private set; } = "+10% offline/lvl(max5)";

        /// <summary>Overrides any non-null/empty strings from a JSON-like flat dictionary.</summary>
        public static void SetOverrides(Dictionary<string, string> overrides)
        {
            if (overrides == null) return;
            foreach (var kv in overrides)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                var field = typeof(L10n).GetProperty(kv.Key);
                if (field != null && field.CanWrite)
                {
                    field.SetValue(null, kv.Value);
                }
            }
        }

        /// <summary>Returns rarity name by index 0-4.</summary>
        public static string RarityName(int r)
        {
            switch (r)
            {
                case 0: return RarityCommon;
                case 1: return RarityMagic;
                case 2: return RarityRare;
                case 3: return RarityLegendary;
                case 4: return RarityArtifact;
                default: return "?";
            }
        }

        /// <summary>Returns slot name by index 0-7.</summary>
        public static string SlotName(int s)
        {
            string[] names = { SlotWeapon, SlotHelmet, SlotArmor, SlotGloves, SlotBoots, SlotRing1, SlotRing2, SlotNeck };
            if (s >= 0 && s < names.Length) return names[s];
            return "?";
        }
    }
}
