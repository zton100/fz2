package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Reforge 重铸：消耗词缀材料，重随机装备全部词缀（保留稀有度与基底）。
func Reforge(p *model.Player, gen *loot.Generator, eq *model.Equipment) error {
	if eq == nil {
		return errors.New("cannot reforge nil equipment")
	}
	cost := map[data.MaterialType]int{}
	for _, a := range eq.Affixes {
		mt := data.AffixMaterialByTier(a.Tier)
		cost[mt] += data.ReforgeCostPerAffix
	}
	for mt, n := range cost {
		if !p.HasMaterial(mt, n) {
			return errors.New("insufficient affix material: " + string(mt))
		}
	}
	for mt, n := range cost {
		p.SpendMaterial(mt, n)
	}
	if len(eq.Affixes) == 0 {
		return nil
	}
	reforged := gen.Generate(eq.Slot, eq.Rarity)
	eq.Affixes = reforged.Affixes
	return nil
}
