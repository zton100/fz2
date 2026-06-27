package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
)

func TestGenerate_NormalRarity_NoAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	eq := g.Generate(data.SlotWeapon, data.RarityCommon, 10)
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.Rarity != data.RarityCommon {
		t.Fatalf("rarity = %d, want common", eq.Rarity)
	}
	if len(eq.Affixes) != 0 {
		t.Fatalf("common affix count = %d, want 0", len(eq.Affixes))
	}
	if eq.Upgrade != 0 {
		t.Fatalf("upgrade = %d, want 0", eq.Upgrade)
	}
	if eq.UID == "" {
		t.Fatal("UID should not be empty")
	}
}

func TestGenerate_MagicRarity_TwoAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(42)))
	eq := g.Generate(data.SlotHelmet, data.RarityMagic, 10)
	// 魔法：1 前缀 + 1 后缀 = 2
	if len(eq.Affixes) != 2 {
		t.Fatalf("magic affix count = %d, want 2", len(eq.Affixes))
	}
	pool := data.BuildAffixPool()
	var prefixes, suffixes int
	for _, a := range eq.Affixes {
		for _, def := range pool {
			if def.Type == a.Type && def.Tier == a.Tier {
				if def.Position == data.PosPrefix {
					prefixes++
				} else {
					suffixes++
				}
				if a.Value < def.Min || a.Value > def.Max {
					t.Fatalf("affix %s value %.4f out of [%.4f,%.4f]", a.Type, a.Value, def.Min, def.Max)
				}
			}
		}
	}
	if prefixes != 1 || suffixes != 1 {
		t.Fatalf("prefixes=%d suffixes=%d, want 1/1", prefixes, suffixes)
	}
}

func TestGenerate_RareRarity_FourAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(7)))
	eq := g.Generate(data.SlotArmor, data.RarityRare, 10)
	// 稀有：2 前缀 + 2 后缀 = 4
	if len(eq.Affixes) != 4 {
		t.Fatalf("rare affix count = %d, want 4", len(eq.Affixes))
	}
}

func TestGenerate_AffixValueWithinRange(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(100)))
	pool := data.BuildAffixPool()
	for i := 0; i < 50; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityRare, 10)
		for _, a := range eq.Affixes {
			var def *data.AffixDef
			for j := range pool {
				if pool[j].Type == a.Type && pool[j].Tier == a.Tier {
					def = &pool[j]
					break
				}
			}
			if def == nil {
				t.Fatalf("affix %s tier %d not found in pool", a.Type, a.Tier)
			}
			if a.Value < def.Min || a.Value > def.Max {
				t.Fatalf("affix %s value %.4f out of [%.4f,%.4f]", a.Type, a.Value, def.Min, def.Max)
			}
		}
	}
}

func TestGenerate_UIDUnique(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	seen := map[string]bool{}
	for i := 0; i < 100; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityCommon, 10)
		if seen[eq.UID] {
			t.Fatal("duplicate UID generated")
		}
		seen[eq.UID] = true
	}
}

