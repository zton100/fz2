package data

import "testing"

func TestBuildAffixPool_ContainsOnlyImplementedAffixes(t *testing.T) {
	pool := BuildAffixPool()
	if len(pool) != 100 {
		t.Fatalf("pool size = %d, want 20 active affixes x 5 tiers = 100", len(pool))
	}
	unsupported := map[AffixType]bool{
		ATExpBonus: true, ATMoveSpeed: true, ATCooldownRed: true,
	}
	for _, affix := range pool {
		if unsupported[affix.Type] {
			t.Fatalf("unsupported affix %s should not appear in the drop pool", affix.Type)
		}
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
	if len(prefixes)+len(suffixes) != len(pool) {
		t.Fatalf("prefix %d + suffix %d != pool size %d", len(prefixes), len(suffixes), len(pool))
	}
	for _, a := range prefixes {
		if a.Position != PosPrefix {
			t.Fatal("prefix filter returned non-prefix affix")
		}
	}
}

// TestBuildAffixPool_ConsistentTierMultiplier verifies that every affix type
// has roughly the same T5/T1 midpoint ratio (within ±2 of target 16x),
// preventing some affix types from becoming disproportionately powerful at
// higher tiers.
func TestBuildAffixPool_ConsistentTierMultiplier(t *testing.T) {
	pool := BuildAffixPool()
	type tierMid struct{ t1, t5 float64 }
	mids := map[AffixType]tierMid{}
	for _, a := range pool {
		mid := (a.Min + a.Max) / 2.0
		entry := mids[a.Type]
		switch a.Tier {
		case 1:
			entry.t1 = mid
		case 5:
			entry.t5 = mid
		}
		mids[a.Type] = entry
	}
	const targetRatio = 16.0
	const tol = 2.0
	for typ, m := range mids {
		if m.t1 <= 0 {
			t.Fatalf("affix %s t1 midpoint = %.4f, positive required", typ, m.t1)
		}
		ratio := m.t5 / m.t1
		if ratio < targetRatio-tol || ratio > targetRatio+tol {
			t.Errorf("affix %s T5/T1 = %.2f, want %.0f\u00b1%.0f", typ, ratio, targetRatio, tol)
		}
	}
}
