package starter

import (
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

const starterWeaponStrength = 45

// GrantLoadout gives a reset player enough equipment to enter the first loop.
func GrantLoadout(p *model.Player, gen *loot.Generator) {
	if p.Equipped == nil {
		p.Equipped = map[data.Slot]*model.Equipment{}
	}
	if p.Materials == nil {
		p.Materials = map[data.MaterialType]int{}
	}
	rarity := reincarnation.QualityFloor(p)
	for _, slot := range data.AllSlots() {
		eq := gen.Generate(slot, rarity, p.Floor)
		if slot == data.SlotWeapon && eq.BaseStats[data.ATStrength] < starterWeaponStrength {
			eq.BaseStats[data.ATStrength] = starterWeaponStrength
		}
		p.Equipped[slot] = eq
	}
	if p.Materials[data.MatBase] < data.ComposeCost {
		p.Materials[data.MatBase] = data.ComposeCost
	}
}
