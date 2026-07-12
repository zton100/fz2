package crafting

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestDecompose_CommonGivesBaseMat(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon}
	yield, err := Decompose(p, eq)
	if err != nil {
		t.Fatalf("decompose error: %v", err)
	}
	if yield[data.MatBase] != 2 {
		t.Fatalf("base mat = %d, want 2", yield[data.MatBase])
	}
	if p.Materials[data.MatBase] != 2 {
		t.Fatalf("player base mat = %d, want 2", p.Materials[data.MatBase])
	}
}

func TestDecompose_RareWithAffixesGivesAffixMat(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{
		UID: "e2", Slot: data.SlotHelmet, Rarity: data.RarityRare,
		Affixes: []model.AffixInstance{
			{Type: data.ATStrength, Tier: 3, Value: 10},
			{Type: data.ATCritRate, Tier: 2, Value: 0.03},
			{Type: data.ATMaxHP, Tier: 3, Value: 50},
			{Type: data.ATArmor, Tier: 2, Value: 15},
		},
	}
	yield, _ := Decompose(p, eq)
	if yield[data.MatBase] != 8 {
		t.Fatalf("base mat = %d, want 8", yield[data.MatBase])
	}
	if yield[data.MatAffixT3] != 2 {
		t.Fatalf("affix t3 mat = %d, want 2", yield[data.MatAffixT3])
	}
	if yield[data.MatAffixT2] != 2 {
		t.Fatalf("affix t2 mat = %d, want 2", yield[data.MatAffixT2])
	}
}

func TestDecompose_UpgradedEquipmentRefundsUpgradeMaterials(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{UID: "e3", Slot: data.SlotWeapon, Rarity: data.RarityMagic, Upgrade: 3}
	yield, err := Decompose(p, eq)
	if err != nil {
		t.Fatalf("decompose error: %v", err)
	}
	wantRefund := int(float64(data.UpgradeCostTable[1]+data.UpgradeCostTable[2]+data.UpgradeCostTable[3]) * data.UpgradeRefundRate)
	wantBase := data.DecomposeBaseYield[data.RarityMagic] + wantRefund
	if yield[data.MatBase] != wantBase {
		t.Fatalf("base mat = %d, want %d", yield[data.MatBase], wantBase)
	}
	if p.Materials[data.MatBase] != wantBase {
		t.Fatalf("player base mat = %d, want %d", p.Materials[data.MatBase], wantBase)
	}
}

func TestDecompose_AppliesEquippedResourceGain(t *testing.T) {
	p := model.NewPlayer("t")
	p.Equipped[data.SlotNeck] = &model.Equipment{
		Affixes: []model.AffixInstance{{Type: data.ATResourceGain, Value: 0.50}},
	}
	eq := &model.Equipment{UID: "resource-target", Slot: data.SlotWeapon, Rarity: data.RarityCommon}
	yield, err := Decompose(p, eq)
	if err != nil {
		t.Fatalf("decompose error: %v", err)
	}
	if yield[data.MatBase] != 3 || p.Materials[data.MatBase] != 3 {
		t.Fatalf("resource-adjusted base material = %d inventory=%d, want 3", yield[data.MatBase], p.Materials[data.MatBase])
	}
}

func TestUpgradeRefund_ClampsAboveCostTable(t *testing.T) {
	eq := &model.Equipment{UID: "e4", Upgrade: 99}
	refund := UpgradeRefund(eq)
	if refund <= 0 {
		t.Fatalf("refund = %d, want positive", refund)
	}
}

func TestDecompose_NilError(t *testing.T) {
	p := model.NewPlayer("t")
	_, err := Decompose(p, nil)
	if err == nil {
		t.Fatal("decompose nil should error")
	}
}
