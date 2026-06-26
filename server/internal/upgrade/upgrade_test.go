package upgrade

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestUpgrade_LowLevelAlwaysSuccess(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 100)
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 0,
	}
	rng := rand.New(rand.NewSource(1))
	result, err := Upgrade(p, rng, eq)
	if err != nil {
		t.Fatalf("upgrade error: %v", err)
	}
	if !result.Success {
		t.Fatal("+1 should always succeed")
	}
	if eq.Upgrade != 1 {
		t.Fatalf("upgrade = %d, want 1", eq.Upgrade)
	}
}

func TestUpgrade_MaxLevelError(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 100)
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 10,
	}
	rng := rand.New(rand.NewSource(1))
	_, err := Upgrade(p, rng, eq)
	if err == nil {
		t.Fatal("should error at max level")
	}
}

func TestUpgrade_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 0,
	}
	rng := rand.New(rand.NewSource(1))
	_, err := Upgrade(p, rng, eq)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}

func TestUpgrade_HighLevelFailNoDegrade(t *testing.T) {
	for seed := 0; seed < 100; seed++ {
		p := model.NewPlayer("t")
		p.AddMaterial(data.MatBase, 100)
		eq := &model.Equipment{
			UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 9,
		}
		rng := rand.New(rand.NewSource(int64(seed)))
		result, _ := Upgrade(p, rng, eq)
		if !result.Success {
			if eq.Upgrade == 9 {
				return // 观察到失败且不掉级，通过
			}
		}
	}
	t.Fatal("should observe a failure with no degrade at +9->+10")
}
