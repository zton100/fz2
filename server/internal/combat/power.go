package combat

import "equipment-idle-server/internal/data"

// ComputePower 战力公式：
//
//	战力 = 攻击 × 攻速 × 命中系数 × (1 + 暴击期望) × 生存系数
//	攻击 = 力量 + 敏捷/智力贡献 + 元素伤害 + 反伤贡献
//	攻速 = 1 + attack_speed + 敏捷贡献
//	暴击期望 = crit_rate × crit_damage
//	生存系数包含生命、体力、护盾、护甲、闪避、吸血与击杀恢复。
func ComputePower(s Stats) float64 {
	strength := s[data.ATStrength]
	agility := s[data.ATAgility]
	intellect := s[data.ATIntellect]
	elemental := s[data.ATFireDmg] + s[data.ATColdDmg] + s[data.ATLightningDmg]
	attack := strength + agility*0.35 + intellect*0.40
	attack += elemental * (1.0 + intellect/200.0)
	attack += s[data.ATArmor] * s[data.ATReflect]
	attackSpeed := 1.0 + s[data.ATAttackSpeed] + agility/500.0
	accuracy := 1.0 + s[data.ATAccuracy]*0.5
	critExpectation := s[data.ATCritRate] * s[data.ATCritDamage]
	effectiveHP := s[data.ATMaxHP] + s[data.ATVitality]*5.0 + s[data.ATShield] + s[data.ATKillHeal]*2.0
	survival := (1.0 + effectiveHP/100.0) * (1.0 + s[data.ATArmor]/100.0)
	survival *= 1.0 + s[data.ATEvasion]
	survival *= 1.0 + s[data.ATLifesteal]*4.0
	return attack * attackSpeed * accuracy * (1.0 + critExpectation) * survival
}
