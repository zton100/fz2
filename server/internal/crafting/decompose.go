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
	if refund := UpgradeRefund(eq); refund > 0 {
		yield[data.MatBase] += refund
	}
	for _, a := range eq.Affixes {
		mt := data.AffixMaterialByTier(a.Tier)
		yield[mt]++
	}
	for mt, n := range yield {
		p.AddMaterial(mt, n)
	}
	return yield, nil
}

func UpgradeRefund(eq *model.Equipment) int {
	if eq == nil || eq.Upgrade <= 0 {
		return 0
	}
	maxLevel := eq.Upgrade
	if maxLevel >= len(data.UpgradeCostTable) {
		maxLevel = len(data.UpgradeCostTable) - 1
	}
	spent := 0
	for level := 1; level <= maxLevel; level++ {
		spent += data.UpgradeCostTable[level]
	}
	return int(float64(spent) * data.UpgradeRefundRate)
}
