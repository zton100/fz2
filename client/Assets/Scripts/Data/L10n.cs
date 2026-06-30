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
        public static string UIStatusDisconnected { get; private set; } = "未连接";
        public static string UIStatusConnecting    { get; private set; } = "连接中...";
        public static string UIStatusConnected     { get; private set; } = "已连接";
        public static string UINoSync              { get; private set; } = "尚未同步";
        public static string UIPower               { get; private set; } = "战力：{0:F1}";
        public static string UIPowerDeltaUp        { get; private set; } = "战力：{0:F1} <color=#4f4>+{1:F1}</color>";
        public static string UIPowerDeltaDown      { get; private set; } = "战力：{0:F1} <color=#f44>{1:F1}</color>";
        public static string UIPowerLabel          { get; private set; } = "战力：{0:F1}";
        public static string UIAccount             { get; private set; } = "账号：";
        public static string UIConnect             { get; private set; } = "连接";
        public static string UIBackpack            { get; private set; } = "背包：";
        public static string UIEquipped            { get; private set; } = "已穿戴（{0}/8）：";
        public static string UIEmptySlot           { get; private set; } = "空槽位";
        public static string UIEquipBest           { get; private set; } = "一键穿戴";
        public static string UIEquipBestDone       { get; private set; } = "<color=#4f4>已穿戴 {0} 件提升装备</color>";
        public static string UIEquip               { get; private set; } = "穿戴";
        public static string UIUnequip             { get; private set; } = "卸下";
        public static string UIDecompose           { get; private set; } = "分解";
        public static string UIDecomposeWeak       { get; private set; } = "分解弱装";
        public static string UIDecomposeWeakDone   { get; private set; } = "<color=#4f4>已分解 {0} 件装备</color>";
        public static string UIReforge             { get; private set; } = "重铸";
        public static string UIUpgrade             { get; private set; } = "强化";
        public static string UIWorkshop            { get; private set; } = "工坊";
        public static string UIMaterials           { get; private set; } = "材料：";
        public static string UIComposeLabel        { get; private set; } = "合成（消耗 10 基础材料）：";
        public static string UIReincarnation       { get; private set; } = "转生";
        public static string UISouls               { get; private set; } = "魂点：{0}  最高层：{1}  可转生：{2}";
        public static string UIReincarnate         { get; private set; } = "转生";
        public static string UITalentsLabel        { get; private set; } = "天赋（每级消耗 1 魂点）：";
        public static string UIStage               { get; private set; } = "装备放置 - 第6阶段";
        public static string UIStuckPrefix         { get; private set; } = "卡关：战力 {0:F0} < 怪物 {1:F0}（第 {2} 层）";
        public static string UIOfflineTitle        { get; private set; } = "离线收益";
        public static string UIOfflineDuration     { get; private set; } = "离线时长：{0}（最多结算 8 小时）";
        public static string UIOfflineLoot         { get; private set; } = "获得装备：{0} 件";
        public static string UIOfflineFloors       { get; private set; } = "推进层数：{0}";
        public static string UIOfflineTicks        { get; private set; } = "模拟战斗：{0} 次";
        public static string UIOK                  { get; private set; } = "确定";
        public static string UILootToast           { get; private set; } = "[掉落] {0} {1}";

        // ── Talent names & desc ──
        public static string TalentDamage      { get; private set; } = "damage";
        public static string TalentQuality      { get; private set; } = "quality";
        public static string TalentDrop         { get; private set; } = "drop";
        public static string TalentOfflineGain  { get; private set; } = "offline_gain";
        public static string TalentDamageDesc   { get; private set; } = "每级伤害 +5%（最高10级）";
        public static string TalentQualityDesc  { get; private set; } = "每级掉落品质下限 +1（最高3级）";
        public static string TalentDropDesc     { get; private set; } = "每级高稀有度权重 +3%（最高10级）";
        public static string TalentOfflineDesc  { get; private set; } = "每级离线收益 +10%（最高5级）";

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
