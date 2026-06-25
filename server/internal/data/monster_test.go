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
