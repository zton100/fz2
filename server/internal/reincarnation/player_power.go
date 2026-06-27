package reincarnation

import (
	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/model"
)

// ComputePlayerPower 统一玩家战力计算：基础战力 × (1 + damage 天赋加成)。
// 所有需要玩家战力的地方（在线战斗/离线结算/推送显示）都应使用此函数，
// 确保 damage 天赋在战斗中真正生效，而非仅影响 UI 显示。
func ComputePlayerPower(p *model.Player) float64 {
	stats := combat.AggregateStats(p.EquippedList())
	power := combat.ComputePower(stats)
	return power * (1.0 + DamageBonus(p))
}
