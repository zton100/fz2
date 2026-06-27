package crafting

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestReforge_ChangesAffixes(t *testing.T) {
	p := model.NewPlayer("t")
	// Give all affix material tiers to ensure enough
	for _, mt := range []data.MaterialType{data.MatAffixT1, data.MatAffixT2, data.MatAffixT3, data.MatAffixT4, data.MatAffixT5} {
		p.AddMaterial(mt, 10)
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(42)))
	eq := gen.Generate(data.SlotWeapon, data.RarityMagic, 10)
	oldAffixes := make([]model.AffixInstance, len(eq.Affixes))
	copy(oldAffixes, eq.Affixes)
	err := Reforge(p, gen, eq)
	if err != nil {
		t.Fatalf("reforge error: %v", err)
	}
	if eq.Rarity != data.RarityMagic {
		t.Fatal("reforge changed rarity")
	}
	sameCount := 0
	for i := range oldAffixes {
		if i < len(eq.Affixes) && eq.Affixes[i].Type == oldAffixes[i].Type && eq.Affixes[i].Tier == oldAffixes[i].Tier {
			sameCount++
		}
	}
	if sameCount == len(oldAffixes) {
		t.Fatal("reforge should change affixes (fixed seed)")
	}
}

func TestReforge_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := gen.Generate(data.SlotWeapon, data.RarityRare, 10)
	err := Reforge(p, gen, eq)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}
