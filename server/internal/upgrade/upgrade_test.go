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
				return // 瑙傚療鍒板け璐ヤ笖涓嶆帀绾э紝閫氳繃
			}
		}
	}
	t.Fatal("should observe a failure with no degrade at +9->+10")
}

// TestUpgrade_RatesNonIncreasing verifies success rates are non-increasing
// at higher levels (the curve should get harder, not easier).
func TestUpgrade_RatesNonIncreasing(t *testing.T) {
	for i := 2; i <= 10; i++ {
		if data.UpgradeSuccessRate[i] > data.UpgradeSuccessRate[i-1] {
			t.Errorf("rate at +%d (%.2f) should <= rate at +%d (%.2f)", i, data.UpgradeSuccessRate[i], i-1, data.UpgradeSuccessRate[i-1])
		}
	}
}

// TestUpgrade_ExpectedCostReasonable verifies the total expected material cost
// to fully upgrade (+0 to +10) falls in a reasonable range (1500-3000 base mats).
func TestUpgrade_ExpectedCostReasonable(t *testing.T) {
	totalAttempts := 0.0
	for i := 1; i <= 10; i++ {
		if data.UpgradeSuccessRate[i] <= 0 {
			t.Fatalf("rate at +%d is zero, cannot compute", i)
		}
		totalAttempts += float64(data.UpgradeCostTable[i]) / data.UpgradeSuccessRate[i]
	}
	if totalAttempts < 1500 || totalAttempts > 3000 {
		t.Errorf("expected total attempts to +10 = %.0f, want 1500-3000", totalAttempts)
	}
}
