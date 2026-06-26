package crafting

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestCompose_Success(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 20)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq, err := Compose(p, gen, data.SlotWeapon)
	if err != nil {
		t.Fatalf("compose error: %v", err)
	}
	if eq == nil {
		t.Fatal("compose returned nil")
	}
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.Rarity != data.RarityCommon {
		t.Fatalf("rarity = %d, want common", eq.Rarity)
	}
	if p.Materials[data.MatBase] != 10 {
		t.Fatalf("remaining base mat = %d, want 10", p.Materials[data.MatBase])
	}
}

func TestCompose_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 5)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	_, err := Compose(p, gen, data.SlotWeapon)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}
