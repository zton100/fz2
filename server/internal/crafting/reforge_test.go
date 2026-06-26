package crafting

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestReforge_RerollsAffixes(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatAffixT1, 10)
	p.AddMaterial(data.MatAffixT2, 10)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := gen.Generate(data.SlotWeapon, data.RarityMagic)
	for _, a := range eq.Affixes {
		p.AddMaterial(data.AffixMaterialByTier(a.Tier), 5)
	}
	err := Reforge(p, gen, eq)
	if err != nil {
		t.Fatalf("reforge error: %v", err)
	}
	if len(eq.Affixes) != 2 {
		t.Fatalf("affix count = %d, want 2", len(eq.Affixes))
	}
	if eq.Rarity != data.RarityMagic {
		t.Fatalf("rarity changed")
	}
	if eq.BaseID != "base_weapon" {
		t.Fatalf("base changed")
	}
}

func TestReforge_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := gen.Generate(data.SlotWeapon, data.RarityRare)
	err := Reforge(p, gen, eq)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}

func TestReforge_NoAffixesNoCost(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := &model.Equipment{
		UID: "e1", BaseID: "base_weapon", Slot: data.SlotWeapon,
		Rarity: data.RarityCommon, BaseStats: map[data.AffixType]float64{data.ATStrength: 5},
	}
	err := Reforge(p, gen, eq)
	if err != nil {
		t.Fatalf("reforge no-affix error: %v", err)
	}
}
