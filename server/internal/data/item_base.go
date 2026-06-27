package data

import "equipment-idle-server/internal/locale"

// ItemBase 装备基底：某槽位的白板装备定义。
type ItemBase struct {
	ID        string
	Name      string
	Slot      Slot
	BaseStats map[AffixType]float64
}

// AllBases returns all 8 item bases with names from current locale.
func AllBases() []ItemBase {
	l := locale.Current()
	return []ItemBase{
		{ID: "base_weapon", Name: l.ItemIronSword, Slot: SlotWeapon, BaseStats: map[AffixType]float64{ATStrength: 5}},
		{ID: "base_helmet", Name: l.ItemLeatherHelm, Slot: SlotHelmet, BaseStats: map[AffixType]float64{ATArmor: 3}},
		{ID: "base_armor", Name: l.ItemChainArmor, Slot: SlotArmor, BaseStats: map[AffixType]float64{ATMaxHP: 5}},
		{ID: "base_gloves", Name: l.ItemLeatherGlove, Slot: SlotGloves, BaseStats: map[AffixType]float64{ATAgility: 3}},
		{ID: "base_boots", Name: l.ItemLeatherBoot, Slot: SlotBoots, BaseStats: map[AffixType]float64{ATAgility: 3}},
		{ID: "base_ring", Name: l.ItemIronRing, Slot: SlotRing1, BaseStats: map[AffixType]float64{ATIntellect: 2}},
		{ID: "base_ring2", Name: l.ItemIronRing, Slot: SlotRing2, BaseStats: map[AffixType]float64{ATIntellect: 2}},
		{ID: "base_neck", Name: l.ItemAmulet, Slot: SlotNeck, BaseStats: map[AffixType]float64{ATVitality: 2}},
	}
}

// BaseBySlot returns the item base for a given slot.
func BaseBySlot(slot Slot) ItemBase {
	bases := AllBases()
	for _, b := range bases {
		if b.Slot == slot {
			return b
		}
	}
	return bases[0]
}
