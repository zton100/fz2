package combat

import "equipment-idle-server/internal/data"

// ComputePower 战力公式：
//   战力 = 攻击 × 攻速 × (1 + 暴击期望) × 生存系数
//   攻击 = 力量 + 火伤 + 冰伤 + 雷伤
//   攻速 = 1 + attack_speed
//   暴击期望 = crit_rate × crit_damage
//   生存系数 = (1 + 生命/100) × (1 + 护甲/100)
func ComputePower(s Stats) float64 {
	attack := s[data.ATStrength] + s[data.ATFireDmg] + s[data.ATColdDmg] + s[data.ATLightningDmg]
	attackSpeed := 1.0 + s[data.ATAttackSpeed]
	critExpectation := s[data.ATCritRate] * s[data.ATCritDamage]
	survival := (1.0 + s[data.ATMaxHP]/100.0) * (1.0 + s[data.ATArmor]/100.0)
	return attack * attackSpeed * (1.0 + critExpectation) * survival
}
