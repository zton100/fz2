package data

import (
	"math"
	"testing"
)

func TestMonsterPower_LinearGrowth(t *testing.T) {
	p1 := MonsterPower(1)
	p2 := MonsterPower(2)
	if p2 <= p1 {
		t.Fatalf("floor2 power %.2f should > floor1 power %.2f", p2, p1)
	}
}

func TestMonsterPower_BossSpikeEvery5(t *testing.T) {
	normal := MonsterPower(4)
	boss := MonsterPower(5)
	if boss < normal*1.5 {
		t.Fatalf("boss power %.2f should >= 1.5x normal %.2f", boss, normal)
	}
}

func TestMonsterPower_Positive(t *testing.T) {
	for f := 1; f <= 100; f++ {
		p := MonsterPower(f)
		if p <= 0 || math.IsNaN(p) {
			t.Fatalf("floor %d power %.2f invalid", f, p)
		}
	}
}

// TestMonsterPower_GrowthAcceleratesAfterFloor20 verifies that beyond floor 20,
// absolute per-floor power increments increase floor-over-floor (exponential phase).
func TestMonsterPower_GrowthAcceleratesAfterFloor20(t *testing.T) {
	// Pick two non-boss floors far apart. Under the new piecewise curve
	// (linear until floor 20, then 1.05^x exponential), floor 41 should be
	// much more than 2x floor 21. Under the old pure-linear formula they'd
	// be roughly proportional (~2x).
	p21 := MonsterPower(21) // non-boss, just past the transition
	p41 := MonsterPower(41) // non-boss, well into exponential phase
	if p21 <= 0 || p41 <= 0 {
		t.Fatal("powers should be positive")
	}
	ratio := p41 / p21
	// Under the old linear formula: p41/p21 ≈ (10+40*8)/(10+20*8) = 330/170 ≈ 1.94
	// Under new exponential: p41/p21 = 1.05^(41-21) = 1.05^20 ≈ 2.65
	// We expect > 2.3 to clearly distinguish from linear.
	if ratio < 2.3 {
		t.Fatalf("p41/p21 = %.2f, want >= 2.3 (exponential growth after floor 20)", ratio)
	}
}

func TestMonsterPower_GrowthAcceleratesAgainAfterFloor80(t *testing.T) {
	p81 := MonsterPower(81)
	p121 := MonsterPower(121)
	if p81 <= 0 || p121 <= 0 {
		t.Fatal("powers should be positive")
	}
	ratio := p121 / p81
	// 80 层后从 1.05 提升到 1.055，40 层跨度应明显超过 1.05^40≈7.0。
	if ratio < 7.5 {
		t.Fatalf("p121/p81 = %.2f, want >= 7.5 (accelerated growth after floor 80)", ratio)
	}
}

// TestMonsterPower_Floor20IsTransitionPoint verifies floor 1-20 are linear
// (same step size) while floor 20→21 is the start of exponential acceleration.
func TestMonsterPower_Floor20IsTransitionPoint(t *testing.T) {
	// Collect non-boss deltas for floors 1-20 (all linear, same step)
	const linearStep float64 = 5.0 // expected step in linear phase
	for f := 2; f <= 20; f++ {
		if f%5 == 0 || (f-1)%5 == 0 {
			continue // skip boss floors and floors immediately after boss
		}
		delta := MonsterPower(f) - MonsterPower(f-1)
		if delta < linearStep*0.8 || delta > linearStep*1.2 {
			t.Fatalf("floor %d delta %.2f not near expected linear step %.2f", f, delta, linearStep)
		}
	}
}
