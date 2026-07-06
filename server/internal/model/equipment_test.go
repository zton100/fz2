package model

import (
	"testing"

	"equipment-idle-server/internal/data"
)

func TestEquipment_AllStats_AppliesUpgradeMultiplier(t *testing.T) {
	eq := &Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10},
		Affixes: []AffixInstance{
			{Type: data.ATArmor, Value: 20},
		},
		Upgrade: 3,
	}

	stats := eq.AllStats()
	if stats[data.ATStrength] != 13 {
		t.Fatalf("strength = %.1f, want 13.0", stats[data.ATStrength])
	}
	if stats[data.ATArmor] != 26 {
		t.Fatalf("armor = %.1f, want 26.0", stats[data.ATArmor])
	}
}
