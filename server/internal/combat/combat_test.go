package combat

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestAggregateStats_SumsAcrossEquipment(t *testing.T) {
	eqs := []*model.Equipment{
		{BaseStats: map[data.AffixType]float64{data.ATStrength: 5}, Affixes: []model.AffixInstance{
			{Type: data.ATStrength, Value: 10}}},
		{BaseStats: map[data.AffixType]float64{data.ATStrength: 3}, Affixes: []model.AffixInstance{
			{Type: data.ATCritRate, Value: 0.05}}},
	}
	stats := AggregateStats(eqs)
	if stats[data.ATStrength] != 18 { // 5+10+3
		t.Fatalf("strength = %.2f, want 18", stats[data.ATStrength])
	}
	if stats[data.ATCritRate] != 0.05 {
		t.Fatalf("crit_rate = %.4f, want 0.05", stats[data.ATCritRate])
	}
}

func TestAggregateStats_Empty(t *testing.T) {
	stats := AggregateStats(nil)
	if len(stats) != 0 {
		t.Fatalf("empty stats len = %d, want 0", len(stats))
	}
}

func TestComputePower_BasicCase(t *testing.T) {
	stats := Stats{
		data.ATStrength:    20, // 攻击=20
		data.ATAttackSpeed: 0.0,
		data.ATCritRate:    0.0,
		data.ATCritDamage:  0.0,
		data.ATMaxHP:       0,
		data.ATArmor:       0,
	}
	p := ComputePower(stats)
	// 攻击20 × 攻速1 × (1+0) × (1+0)×(1+0) = 20
	if p != 20 {
		t.Fatalf("power = %.2f, want 20", p)
	}
}

func TestComputePower_WithCritAndSpeed(t *testing.T) {
	stats := Stats{
		data.ATStrength:    10,  // 攻击=10
		data.ATAttackSpeed: 0.5, // 攻速=1.5
		data.ATCritRate:    0.5,
		data.ATCritDamage:  1.0, // 暴击期望=0.5
		data.ATMaxHP:       100, // 生存系数=(1+1)=2
		data.ATArmor:       0,
	}
	p := ComputePower(stats)
	// 10 × 1.5 × (1+0.5) × 2 × (1+0) = 45
	if p != 45 {
		t.Fatalf("power = %.2f, want 45", p)
	}
}

func TestComputePower_ElementalDamageAddsAttack(t *testing.T) {
	stats := Stats{
		data.ATStrength:     10,
		data.ATFireDmg:      5,
		data.ATColdDmg:      5,
		data.ATLightningDmg: 5, // 攻击=10+5+5+5=25
		data.ATAttackSpeed:  0.0,
		data.ATCritRate:     0.0,
		data.ATCritDamage:   0.0,
		data.ATMaxHP:        0,
		data.ATArmor:        0,
	}
	p := ComputePower(stats)
	if p != 25 {
		t.Fatalf("power = %.2f, want 25", p)
	}
}

func TestComputePower_AllCombatAffixesContribute(t *testing.T) {
	base := Stats{data.ATStrength: 10, data.ATArmor: 10}
	basePower := ComputePower(base)
	tests := []struct {
		name  string
		affix data.AffixType
		value float64
	}{
		{name: "agility", affix: data.ATAgility, value: 10},
		{name: "intellect", affix: data.ATIntellect, value: 10},
		{name: "vitality", affix: data.ATVitality, value: 10},
		{name: "accuracy", affix: data.ATAccuracy, value: 0.10},
		{name: "evasion", affix: data.ATEvasion, value: 0.10},
		{name: "lifesteal", affix: data.ATLifesteal, value: 0.05},
		{name: "kill heal", affix: data.ATKillHeal, value: 10},
		{name: "reflect", affix: data.ATReflect, value: 0.10},
		{name: "shield", affix: data.ATShield, value: 10},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			stats := Stats{}
			for affix, value := range base {
				stats[affix] = value
			}
			stats[tt.affix] = tt.value
			if power := ComputePower(stats); power <= basePower {
				t.Fatalf("power with %s = %.2f, want above baseline %.2f", tt.affix, power, basePower)
			}
		})
	}
}
