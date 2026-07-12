package reincarnation

import (
	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"math"
)

// ComputePlayerPower 统一玩家战力计算：基础战力 × (1 + damage 天赋加成)。
// 所有需要玩家战力的地方（在线战斗/离线结算/推送显示）都应使用此函数，
// 确保 damage 天赋在战斗中真正生效，而非仅影响 UI 显示。
func ComputePlayerPower(p *model.Player) float64 {
	stats := combat.AggregateStats(p.EquippedList())
	power := combat.ComputePower(stats)
	return power * (1.0 + DamageBonus(p)) * (1.0 + LegendaryPowerBonus(p))
}

// LegendaryPowerBonus sums fixed global-power bonuses from equipped legendary
// definitions. Additive stacking keeps multiple fixed effects predictable.
func LegendaryPowerBonus(p *model.Player) float64 {
	bonus := 0.0
	for _, eq := range p.EquippedList() {
		if def, ok := data.LegendaryByID(eq.LegendaryID); ok {
			bonus += def.PowerMultiplier
		}
	}
	return bonus
}

// ProjectedPowerIfEquipped returns the player's authoritative total power if
// eq replaced the currently equipped item in the same slot. The loadout is
// restored before returning; callers must hold the player's mutation lock.
func ProjectedPowerIfEquipped(p *model.Player, eq *model.Equipment) float64 {
	if eq == nil {
		return ComputePlayerPower(p)
	}
	current, hadCurrent := p.Equipped[eq.Slot]
	p.Equipped[eq.Slot] = eq
	projected := ComputePlayerPower(p)
	if hadCurrent {
		p.Equipped[eq.Slot] = current
	} else {
		delete(p.Equipped, eq.Slot)
	}
	return projected
}

// PowerScoreIfEquipped returns the real power contributed by eq over an empty
// slot in the player's current loadout. Score differences between same-slot
// items equal the actual total-power change when replacing that slot.
func PowerScoreIfEquipped(p *model.Player, eq *model.Equipment) float64 {
	if eq == nil {
		return 0
	}
	current, hadCurrent := p.Equipped[eq.Slot]
	delete(p.Equipped, eq.Slot)
	baseline := ComputePlayerPower(p)
	p.Equipped[eq.Slot] = eq
	withEquipment := ComputePlayerPower(p)
	if hadCurrent {
		p.Equipped[eq.Slot] = current
	} else {
		delete(p.Equipped, eq.Slot)
	}
	return withEquipment - baseline
}

// EquipmentStatTotal returns an aggregated equipped affix value after upgrade
// multipliers. It is shared by non-combat economy effects.
func EquipmentStatTotal(p *model.Player, affix data.AffixType) float64 {
	stats := combat.AggregateStats(p.EquippedList())
	return stats[affix]
}

// ApplyResourceGain applies equipped resource_gain to a positive material
// reward. Rounding keeps material inventories integral.
func ApplyResourceGain(p *model.Player, base int) int {
	if base <= 0 {
		return 0
	}
	bonus := EquipmentStatTotal(p, data.ATResourceGain)
	return int(math.Round(float64(base) * (1.0 + bonus)))
}

// ApplyBossReward combines ordinary resource gain with fixed legendary
// first-clear bonuses. Both are additive so the reward preview stays stable.
func ApplyBossReward(p *model.Player, base int) int {
	if base <= 0 {
		return 0
	}
	bonus := EquipmentStatTotal(p, data.ATResourceGain)
	for _, eq := range p.EquippedList() {
		if def, ok := data.LegendaryByID(eq.LegendaryID); ok {
			bonus += def.BossRewardMultiplier
		}
	}
	return int(math.Round(float64(base) * (1.0 + bonus)))
}
