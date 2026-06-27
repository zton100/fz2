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

func TestDropRandomSlotModified_QualityFloorPreventsCommon(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(77)))
	drop := NewDropTable(g)
	mod := DropModifier{DropBonus: 0, QualityFloor: data.RarityMagic}
	for i := 0; i < 50; i++ {
		eq := drop.DropRandomSlotModified(1, mod)
		if eq == nil {
			t.Fatal("drop returned nil")
		}
		if eq.Rarity < data.RarityMagic {
			t.Fatalf("rarity = %d, want >= magic(%d) with quality floor", eq.Rarity, data.RarityMagic)
		}
	}
}

func TestDropRandomSlotModified_DropBonusIncreasesQuality(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(99)))
	drop := NewDropTable(g)
	// 无加成时 floor=1 高稀有度几乎不出
	var rareOrBetterNoBonus int
	for i := 0; i < 200; i++ {
		mod := DropModifier{DropBonus: 0, QualityFloor: 0}
		eq := drop.DropRandomSlotModified(1, mod)
		if eq.Rarity >= data.RarityRare {
			rareOrBetterNoBonus++
		}
	}
	// 有加成时高稀有度显著增多
	var rareOrBetterWithBonus int
	for i := 0; i < 200; i++ {
		mod := DropModifier{DropBonus: 0.3, QualityFloor: 0}
		eq := drop.DropRandomSlotModified(1, mod)
		if eq.Rarity >= data.RarityRare {
			rareOrBetterWithBonus++
		}
	}
	if rareOrBetterWithBonus <= rareOrBetterNoBonus {
		t.Fatalf("drop bonus should increase high rarity: noBonus=%d withBonus=%d", rareOrBetterNoBonus, rareOrBetterWithBonus)
	}
}
