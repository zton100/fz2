package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Decompose 分解一件装备：返回产出材料并加入玩家库存。
// 调用方负责确保装备已从背包/穿戴移除。
func Decompose(p *model.Player, eq *model.Equipment) (map[data.MaterialType]int, error) {
	if eq == nil {
		return nil, errors.New("无法分解空装备")
	}
	yield := map[data.MaterialType]int{}
	yield[data.MatBase] = data.DecomposeBaseYield[eq.Rarity]
	for _, a := range eq.Affixes {
		mt := data.AffixMaterialByTier(a.Tier)
		yield[mt]++
	}
	for mt, n := range yield {
		p.AddMaterial(mt, n)
	}
	return yield, nil
}
