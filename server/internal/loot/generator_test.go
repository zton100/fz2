package loot

import (
	mathrand "math/rand"
	"strings"
	"testing"

	"equipment-idle-server/internal/data"
)

// ── Existing tests from generator_test.go ──

func TestGenerate_NormalRarity_NoAffixes(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(1)))
	eq := g.Generate(data.SlotWeapon, data.RarityCommon, 10)
	if len(eq.Affixes) != 0 {
		t.Fatalf("common has %d affixes, want 0", len(eq.Affixes))
	}
	if eq.UID == "" {
		t.Fatal("common equipment has empty UID")
	}
	if !strings.HasPrefix(eq.UID, "eq_") {
		t.Fatalf("UID %s should start with eq_", eq.UID)
	}
}

func TestGenerate_MagicRarity_TwoAffixes(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(1)))
	eq := g.Generate(data.SlotHelmet, data.RarityMagic, 10)
	if len(eq.Affixes) == 0 {
		t.Fatal("magic should have affixes")
	}
}

func TestGenerate_RareRarity_FourAffixes(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(1)))
	eq := g.Generate(data.SlotArmor, data.RarityRare, 10)
	if len(eq.Affixes) == 0 {
		t.Fatal("rare should have affixes")
	}
}

func TestGenerate_AffixValueWithinRange(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(1)))
	for i := 0; i < 50; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityRare, 10)
		for _, a := range eq.Affixes {
			if a.Value <= 0 {
				t.Fatalf("affix %s value %.4f <= 0", a.Type, a.Value)
			}
		}
	}
}

func TestGenerate_UIDUnique(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(1)))
	uids := map[string]bool{}
	for i := 0; i < 100; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityCommon, 10)
		if uids[eq.UID] {
			t.Fatalf("duplicate UID: %s", eq.UID)
		}
		uids[eq.UID] = true
	}
}

func TestGenerate_SameSlotUsesMultipleItemBases(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(17)))
	baseIDs := map[string]bool{}
	for i := 0; i < 100; i++ {
		baseIDs[g.Generate(data.SlotWeapon, data.RarityCommon, 10).BaseID] = true
	}
	if len(baseIDs) < 3 {
		t.Fatalf("generated weapon base ids = %v, want at least 3 distinct bases", baseIDs)
	}
}

func TestGenerate_LegendaryUsesFixedDefinitionForSlot(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(23)))
	eq := g.Generate(data.SlotWeapon, data.RarityLegendary, 80)

	if eq.LegendaryID == "" {
		t.Fatal("legendary drop has no fixed legendary identity")
	}
	def, ok := data.LegendaryByID(eq.LegendaryID)
	if !ok {
		t.Fatalf("legendary id %q has no data definition", eq.LegendaryID)
	}
	if eq.Name != def.Name || eq.BaseID != def.BaseID || eq.Slot != def.Slot {
		t.Fatalf("generated legendary = %+v, want definition %+v", eq, def)
	}
}

func TestGenerate_LegendaryRotationReachesEveryDefinition(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(29)))
	for _, slot := range data.AllSlots() {
		want := data.LegendariesBySlot(slot)
		seen := map[string]bool{}
		for range want {
			seen[g.Generate(slot, data.RarityLegendary, 80).LegendaryID] = true
		}
		for _, def := range want {
			if !seen[def.ID] {
				t.Fatalf("slot %d never generated legendary %q", slot, def.ID)
			}
		}
	}
}

func TestGenerate_ArtifactDoesNotBorrowLegendaryIdentity(t *testing.T) {
	g := NewGenerator(mathrand.New(mathrand.NewSource(31)))
	eq := g.Generate(data.SlotWeapon, data.RarityArtifact, 120)
	if eq.LegendaryID != "" {
		t.Fatalf("artifact borrowed legendary id %q", eq.LegendaryID)
	}
}

// ── Task 7: UID tests ──

func TestUID_Uniqueness(t *testing.T) {
	seen := map[string]bool{}
	for i := 0; i < 10000; i++ {
		uid := nextUID()
		if seen[uid] {
			t.Fatalf("duplicate UID: %s at iteration %d", uid, i)
		}
		seen[uid] = true
	}
}

func TestUID_Format(t *testing.T) {
	for i := 0; i < 100; i++ {
		uid := nextUID()
		if !strings.HasPrefix(uid, "eq_") {
			t.Errorf("UID %s should start with eq_", uid)
		}
		if len(uid) != 35 {
			t.Errorf("UID %s len=%d, want 35", uid, len(uid))
		}
	}
}

func TestUID_NoSequence(t *testing.T) {
	seen := map[string]bool{}
	rng1 := mathrand.New(mathrand.NewSource(1))
	rng2 := mathrand.New(mathrand.NewSource(2))
	g1 := NewGenerator(rng1)
	g2 := NewGenerator(rng2)
	for i := 0; i < 500; i++ {
		eq1 := g1.Generate(data.SlotWeapon, data.RarityCommon, 10)
		eq2 := g2.Generate(data.SlotWeapon, data.RarityCommon, 10)
		if seen[eq1.UID] {
			t.Fatalf("duplicate UID across generators: %s", eq1.UID)
		}
		seen[eq1.UID] = true
		if seen[eq2.UID] {
			t.Fatalf("duplicate UID across generators: %s", eq2.UID)
		}
		seen[eq2.UID] = true
	}
}
