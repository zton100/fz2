package data

import "testing"

func TestBuildAffixPool_Count115(t *testing.T) {
	pool := BuildAffixPool()
	if len(pool) != 115 {
		t.Fatalf("pool size = %d, want 115", len(pool))
	}
}

func TestBuildAffixPool_TierRangeValid(t *testing.T) {
	pool := BuildAffixPool()
	for _, a := range pool {
		if a.Tier < 1 || a.Tier > 5 {
			t.Fatalf("affix %s tier = %d, want 1..5", a.Type, a.Tier)
		}
		if a.Min > a.Max {
			t.Fatalf("affix %s tier %d min %.4f > max %.4f", a.Type, a.Tier, a.Min, a.Max)
		}
	}
}

func TestAffixesByPosition(t *testing.T) {
	pool := BuildAffixPool()
	prefixes := AffixesByPosition(pool, PosPrefix)
	suffixes := AffixesByPosition(pool, PosSuffix)
	if len(prefixes)+len(suffixes) != 115 {
		t.Fatalf("prefix %d + suffix %d != 115", len(prefixes), len(suffixes))
	}
	for _, a := range prefixes {
		if a.Position != PosPrefix {
			t.Fatal("prefix filter returned non-prefix affix")
		}
	}
}
