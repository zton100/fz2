package dungeon

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func makePlayer() *model.Player {
	return model.NewPlayer("test")
}

func TestRunner_PlayerStronger_AdvancesFloor(t *testing.T) {
	p := makePlayer()
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, nil, loot.NewDropTable(gen))
	startFloor := p.Floor
	r.Tick()
	if p.Floor <= startFloor {
		t.Fatalf("floor = %d, should advance from %d", p.Floor, startFloor)
	}
}

func TestRunner_PlayerWeaker_StaysFloor(t *testing.T) {
	p := makePlayer()
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, nil, loot.NewDropTable(gen))
	startFloor := p.Floor
	r.Tick()
	if p.Floor != startFloor {
		t.Fatalf("floor = %d, should stay at %d", p.Floor, startFloor)
	}
}

func TestRunner_WinDropsEquipment(t *testing.T) {
	p := makePlayer()
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, nil, loot.NewDropTable(gen))
	r.Tick()
	if len(p.EquipBag) == 0 {
		t.Fatal("no equipment dropped after winning a battle")
	}
}

func TestRunner_DamageTalentEnablesWin(t *testing.T) {
	p := makePlayer()
	// 刚好打不过第1层怪物（STR=8 → power ≈8，怪物power=10）
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 8},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, nil, loot.NewDropTable(gen))
	startFloor := p.Floor
	r.Tick()
	if p.Floor != startFloor {
		t.Fatal("should not advance with str=8 (power too low)")
	}
	// 加 damage 天赋后应该能打过
	p.Floor = 1
	p.Talents["damage"] = 10 // +50% damage
	r.Tick()
	if p.Floor <= startFloor {
		t.Fatal("should advance with damage talent boost")
	}
}
