package upgrade

import (
	"errors"
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// MaxUpgrade 强化上限。
const MaxUpgrade = 10

// UpgradeResult 强化结果。
type UpgradeResult struct {
	Success bool // 是否成功
	NewLvl  int  // 强化后的等级
}

// Upgrade 强化一件装备一级。
// 消耗基础材料（按目标等级查表），按成功率判定，失败不掉级。
func Upgrade(p *model.Player, rng *rand.Rand, eq *model.Equipment) (UpgradeResult, error) {
	if eq.Upgrade >= MaxUpgrade {
		return UpgradeResult{}, errors.New("already at max upgrade level")
	}
	targetLvl := eq.Upgrade + 1
	cost := data.UpgradeCostTable[targetLvl]
	if !p.HasMaterial(data.MatBase, cost) {
		return UpgradeResult{}, errors.New("insufficient base material")
	}
	p.SpendMaterial(data.MatBase, cost)

	rate := data.UpgradeSuccessRate[targetLvl]
	if rng.Float64() < rate {
		eq.Upgrade = targetLvl
		return UpgradeResult{Success: true, NewLvl: targetLvl}, nil
	}
	return UpgradeResult{Success: false, NewLvl: eq.Upgrade}, nil
}
