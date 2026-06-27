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
	// 鏃犲姞鎴愭椂 floor=1 楂樼█鏈夊害鍑犱箮涓嶅嚭
	var rareOrBetterNoBonus int
	for i := 0; i < 200; i++ {
		mod := DropModifier{DropBonus: 0, QualityFloor: 0}
		eq := drop.DropRandomSlotModified(1, mod)
		if eq.Rarity >= data.RarityRare {
			rareOrBetterNoBonus++
		}
	}
	// 鏈夊姞鎴愭椂楂樼█鏈夊害鏄捐憲澧炲
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

// TestDrop_FloorGatingLowFloorOnlyBasic verifies that at floor 1,
// only Basic affixes (strength/agility/...) can appear — no Derived or Special.
func TestDrop_FloorGatingLowFloorOnlyBasic(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(42)))
	for i := 0; i < 100; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityRare, 1)
		for _, a := range eq.Affixes {
			cat := data.AffixCategoryOf(a.Type)
			if cat != data.AffixBasic {
				t.Fatalf("floor 1 generated affix %s (cat=%d), want only Basic(0)", a.Type, cat)
			}
		}
	}
}

// TestDrop_FloorGatingUnlocksDerived verifies that at floor 10,
// Basic and Derived affixes can both appear.
func TestDrop_FloorGatingUnlocksDerived(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(99)))
	var sawDerived bool
	for i := 0; i < 200; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityRare, 10)
		for _, a := range eq.Affixes {
			cat := data.AffixCategoryOf(a.Type)
			if cat == data.AffixSpecial {
				t.Fatalf("floor 10 generated special affix %s, should not unlock until floor 16", a.Type)
			}
			if cat == data.AffixDerived {
				sawDerived = true
			}
		}
	}
	if !sawDerived {
		t.Fatal("should have seen at least one Derived affix at floor 10")
	}
}

// TestDrop_FloorGatingAllUnlocked verifies that at floor 20,
// all affix categories (Basic+Derived+Special) can appear.
func TestDrop_FloorGatingAllUnlocked(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(7)))
	var sawSpecial bool
	for i := 0; i < 300; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityLegendary, 20)
		for _, a := range eq.Affixes {
			if data.AffixCategoryOf(a.Type) == data.AffixSpecial {
				sawSpecial = true
			}
		}
	}
	if !sawSpecial {
		t.Fatal("should have seen at least one Special affix at floor 20")
	}
}
