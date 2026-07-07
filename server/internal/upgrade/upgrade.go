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
		return UpgradeResult{}, errors.New("已达到最高强化等级")
	}
	targetLvl := eq.Upgrade + 1
	cost := data.UpgradeCostTable[targetLvl]
	if !p.HasMaterial(data.MatBase, cost) {
		return UpgradeResult{}, errors.New("基础材料不足")
	}
	p.SpendMaterial(data.MatBase, cost)

	rate := data.UpgradeSuccessRate[targetLvl]
	if rng.Float64() < rate {
		eq.Upgrade = targetLvl
		return UpgradeResult{Success: true, NewLvl: targetLvl}, nil
	}
	return UpgradeResult{Success: false, NewLvl: eq.Upgrade}, nil
}

// TransferUpgrade moves upgrade investment between two same-slot items.
// The target receives the source level and the source falls back to the
// target's previous level, so total upgrade levels are not duplicated.
func TransferUpgrade(source, target *model.Equipment) error {
	if source == nil || target == nil {
		return errors.New("装备不存在")
	}
	if source == target || source.UID == target.UID {
		return errors.New("不能继承到同一件装备")
	}
	if source.Slot != target.Slot {
		return errors.New("只能继承同部位装备强化")
	}
	if source.Upgrade <= target.Upgrade {
		return errors.New("来源强化等级不高于目标")
	}
	sourceUpgrade := source.Upgrade
	source.Upgrade = target.Upgrade
	target.Upgrade = sourceUpgrade
	return nil
}
