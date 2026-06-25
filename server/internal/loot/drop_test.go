package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
)

func TestRollRarity_LowFloorMostlyCommon(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	var common int
	for i := 0; i < 100; i++ {
		r := drop.RollRarity(1)
		if r == data.RarityCommon {
			common++
		}
	}
	if common < 50 {
		t.Fatalf("floor1 common drops = %d/100, want >= 50", common)
	}
}

func TestRollRarity_HighFloorCanDropLegendary(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	var highRarity int
	for i := 0; i < 200; i++ {
		r := drop.RollRarity(50)
		if r >= data.RarityLegendary {
			highRarity++
		}
	}
	if highRarity == 0 {
		t.Fatal("floor50 should drop legendary+ at least once in 200 rolls")
	}
}

func TestDrop_GeneratesEquipment(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	eq := drop.Drop(data.SlotWeapon, 10)
	if eq == nil {
		t.Fatal("drop returned nil equipment")
	}
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.UID == "" {
		t.Fatal("dropped equipment has empty UID")
	}
}
