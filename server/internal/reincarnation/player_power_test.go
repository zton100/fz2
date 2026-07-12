package reincarnation

import (
	"testing"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestProjectedPowerIfEquippedUsesRealLoadoutAndRestoresCurrentGear(t *testing.T) {
	current := &model.Equipment{UID: "current", Slot: data.SlotWeapon, BaseStats: map[data.AffixType]float64{data.ATStrength: 10}}
	weaker := &model.Equipment{UID: "weaker", Slot: data.SlotWeapon, BaseStats: map[data.AffixType]float64{data.ATStrength: 2}}
	stronger := &model.Equipment{UID: "stronger", Slot: data.SlotWeapon, BaseStats: map[data.AffixType]float64{data.ATStrength: 20}}
	p := model.NewPlayer("test")
	p.Equipped[data.SlotWeapon] = current

	currentPower := ComputePlayerPower(p)
	if got := ProjectedPowerIfEquipped(p, weaker); got >= currentPower {
		t.Fatalf("weaker projected power = %.1f, want below %.1f", got, currentPower)
	}
	if got := ProjectedPowerIfEquipped(p, stronger); got <= currentPower {
		t.Fatalf("stronger projected power = %.1f, want above %.1f", got, currentPower)
	}
	if p.Equipped[data.SlotWeapon] != current {
		t.Fatal("projected power calculation mutated the equipped loadout")
	}
}

func TestComputePlayerPowerAppliesEquippedLegendaryMultiplier(t *testing.T) {
	eq := &model.Equipment{
		LegendaryID: "legendary_ember_cleaver",
		Slot:        data.SlotWeapon,
		BaseStats:   map[data.AffixType]float64{data.ATStrength: 100},
	}
	p := model.NewPlayer("legendary-power")
	p.Equipped[data.SlotWeapon] = eq

	stats := combat.AggregateStats(p.EquippedList())
	want := combat.ComputePower(stats) * 1.12
	if got := ComputePlayerPower(p); got != want {
		t.Fatalf("legendary power = %.2f, want %.2f", got, want)
	}
}

func TestPowerScoreDifferenceEqualsActualSameSlotUpgrade(t *testing.T) {
	current := &model.Equipment{UID: "current", Slot: data.SlotWeapon, BaseStats: map[data.AffixType]float64{data.ATStrength: 10}}
	upgrade := &model.Equipment{UID: "upgrade", Slot: data.SlotWeapon, BaseStats: map[data.AffixType]float64{data.ATStrength: 20}}
	p := model.NewPlayer("test")
	p.Equipped[data.SlotWeapon] = current

	actualGain := ProjectedPowerIfEquipped(p, upgrade) - ComputePlayerPower(p)
	scoreGain := PowerScoreIfEquipped(p, upgrade) - PowerScoreIfEquipped(p, current)
	if actualGain != scoreGain {
		t.Fatalf("score gain = %.1f, want actual loadout gain %.1f", scoreGain, actualGain)
	}
	if p.Equipped[data.SlotWeapon] != current {
		t.Fatal("power score calculation mutated the equipped loadout")
	}
}
