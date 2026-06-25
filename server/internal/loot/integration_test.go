package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestIntegration_GenerateAggregatePower(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(999)))
	eqs := []*model.Equipment{}
	for _, slot := range data.AllSlots() {
		eq := g.Generate(slot, data.RarityRare)
		eqs = append(eqs, eq)
	}
	stats := combat.AggregateStats(eqs)
	power := combat.ComputePower(stats)

	if power <= 0 {
		t.Fatalf("power = %.2f, want > 0", power)
	}
	// 稀有装备 8 件，战力应有合理下限（白板攻击就 > 0）
	t.Logf("8-slot rare power = %.2f", power)
}
