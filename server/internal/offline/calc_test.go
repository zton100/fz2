package offline

import (
	"math/rand"
	"testing"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestCalc_OfflineDurationCappedAt8Hours(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, nil, drop, 10*time.Hour, 0, 0, 0)
	if result.Duration > 8*time.Hour {
		t.Fatalf("duration = %v, want <= 8h", result.Duration)
	}
}

func TestCalc_StrongPlayerGainsLoot(t *testing.T) {
	p := model.NewPlayer("t")
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, nil, drop, 1*time.Hour, 0, 0, 0)
	if result.TicksSimulated == 0 {
		t.Fatal("should simulate some ticks")
	}
	if result.LootCount == 0 {
		t.Fatal("strong player should gain loot while offline")
	}
	if len(p.EquipBag) != result.LootCount {
		t.Fatalf("bag size = %d, want %d", len(p.EquipBag), result.LootCount)
	}
}

func TestCalc_WeakPlayerNoLoot(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, nil, drop, 1*time.Hour, 0, 0, 0)
	if result.LootCount != 0 {
		t.Fatalf("weak player loot = %d, want 0", result.LootCount)
	}
	if result.TicksSimulated == 0 {
		t.Fatal("should still simulate ticks (just lose)")
	}
}

func TestCalc_ZeroDurationNoOp(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, nil, drop, 0, 0, 0, 0)
	if result.TicksSimulated != 0 {
		t.Fatal("zero duration should simulate 0 ticks")
	}
}

func TestCalc_AdvancesFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 100000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	startFloor := p.Floor
	result := Calc(p, nil, drop, 10*time.Second, 0, 0, 0)
	if p.Floor <= startFloor {
		t.Fatalf("floor = %d, should advance from %d", p.Floor, startFloor)
	}
	if result.FloorsAdvanced == 0 {
		t.Fatal("should report floor advances")
	}
}

func TestCalc_BossFirstClearGrantsBaseMaterials(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	p.MaxFloor = 5
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 100000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)

	Calc(p, nil, drop, 2*time.Second, 0, 0, 0)

	if p.Materials[data.MatBase] != 15 {
		t.Fatalf("base materials = %d, want 15", p.Materials[data.MatBase])
	}
}
