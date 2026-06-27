package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Reforge 重铸指定装备的全部词缀（保留稀有度/基底/强化等级）。
// 消耗词缀材料（按当前词缀类型和 Tier 各 1 个）。
func Reforge(p *model.Player, gen *loot.Generator, eq *model.Equipment) error {
	if eq == nil {
		return errors.New("nil equipment")
	}
	// 统计消耗
	for _, a := range eq.Affixes {
		matType := data.AffixMaterialByTier(a.Tier)
		if !p.HasMaterial(matType, data.ReforgeCostPerAffix) {
			return errors.New("insufficient affix material")
		}
	}
	// 扣除材料
	for _, a := range eq.Affixes {
		matType := data.AffixMaterialByTier(a.Tier)
		p.SpendMaterial(matType, data.ReforgeCostPerAffix)
	}
	// 重新生成词缀（相同稀有度/槽位/楼层）
	reforged := gen.Generate(eq.Slot, eq.Rarity, p.Floor)
	eq.Affixes = reforged.Affixes
	return nil
}
