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
	p.AddMaterial(data.MatBase, data.ComposeCost)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq, err := Compose(p, gen, data.SlotWeapon)
	if err != nil {
		t.Fatalf("compose error: %v", err)
	}
	if eq == nil {
		t.Fatal("compose returned nil")
	}
	if eq.UID == "" {
		t.Fatal("composed equipment has empty UID")
	}
	if eq.Rarity != data.RarityCommon {
		t.Fatalf("compose rarity = %d, want common", eq.Rarity)
	}
	if len(p.EquipBag) != 1 {
		t.Fatalf("bag size = %d, want 1", len(p.EquipBag))
	}
}

func TestCompose_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	_, err := Compose(p, gen, data.SlotWeapon)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}
