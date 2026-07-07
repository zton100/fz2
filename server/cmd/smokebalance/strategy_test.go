package main

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

func TestAutoEquipBestEquipsOnlyPowerGains(t *testing.T) {
	player := model.NewPlayer("strategy")
	current := strategyEquipment("current", data.SlotWeapon, data.RarityCommon, 10)
	weak := strategyEquipment("weak", data.SlotWeapon, data.RarityCommon, 5)
	strong := strategyEquipment("strong", data.SlotWeapon, data.RarityMagic, 20)
	player.Equipped[data.SlotWeapon] = current
	player.EquipBag = []*model.Equipment{weak, strong}

	equipped, gain := autoEquipBest(player)

	if equipped != 1 {
		t.Fatalf("equipped = %d, want 1", equipped)
	}
	if gain <= 0 {
		t.Fatalf("gain = %.1f, want positive", gain)
	}
	if player.Equipped[data.SlotWeapon] != strong {
		t.Fatalf("equipped weapon = %v, want strong item", player.Equipped[data.SlotWeapon])
	}
	if len(player.EquipBag) != 1 || player.EquipBag[0] != weak {
		t.Fatalf("bag = %v, want only weak item retained", player.EquipBag)
	}
}

func TestAutoDecomposeWeakBagKeepsUpgrades(t *testing.T) {
	player := model.NewPlayer("strategy")
	current := strategyEquipment("current", data.SlotWeapon, data.RarityCommon, 10)
	weak := strategyEquipment("weak", data.SlotWeapon, data.RarityRare, 5)
	strong := strategyEquipment("strong", data.SlotWeapon, data.RarityMagic, 20)
	player.Equipped[data.SlotWeapon] = current
	player.EquipBag = []*model.Equipment{weak, strong, nil}

	decomposed := autoDecomposeWeakBag(player)

	if decomposed != 1 {
		t.Fatalf("decomposed = %d, want 1", decomposed)
	}
	if player.Materials[data.MatBase] != data.DecomposeBaseYield[data.RarityRare] {
		t.Fatalf("base materials = %d, want rare yield", player.Materials[data.MatBase])
	}
	if len(player.EquipBag) != 2 || player.EquipBag[0] != strong || player.EquipBag[1] != nil {
		t.Fatalf("bag = %v, want strong item and nil retained", player.EquipBag)
	}
}

func TestBestAffordableUpgradeChoosesHighestPowerPerMaterial(t *testing.T) {
	player := model.NewPlayer("strategy")
	lowGain := strategyEquipment("low", data.SlotWeapon, data.RarityCommon, 10)
	highGain := strategyEquipment("high", data.SlotArmor, data.RarityCommon, 30)
	player.Equipped[data.SlotWeapon] = lowGain
	player.Equipped[data.SlotArmor] = highGain
	player.Materials[data.MatBase] = data.UpgradeCostTable[1]

	best := bestAffordableUpgrade(player)

	if best != highGain {
		t.Fatalf("best upgrade = %v, want high gain item", best)
	}
}

func TestAutoUpgradeEquippedSpendsMaterialsAndRaisesPower(t *testing.T) {
	player := model.NewPlayer("strategy")
	weapon := strategyEquipment("weapon", data.SlotWeapon, data.RarityCommon, 10)
	player.Equipped[data.SlotWeapon] = weapon
	player.Materials[data.MatBase] = data.UpgradeCostTable[1]
	beforePower := reincarnation.ComputePlayerPower(player)

	upgrades := autoUpgradeEquipped(player, rand.New(rand.NewSource(1)))

	if upgrades != 1 {
		t.Fatalf("upgrades = %d, want 1", upgrades)
	}
	if weapon.Upgrade != 1 {
		t.Fatalf("weapon upgrade = %d, want 1", weapon.Upgrade)
	}
	if player.Materials[data.MatBase] != 0 {
		t.Fatalf("base materials = %d, want 0", player.Materials[data.MatBase])
	}
	if afterPower := reincarnation.ComputePlayerPower(player); afterPower <= beforePower {
		t.Fatalf("power after upgrade = %.1f, want > %.1f", afterPower, beforePower)
	}
}

func strategyEquipment(uid string, slot data.Slot, rarity data.Rarity, strength float64) *model.Equipment {
	return &model.Equipment{
		UID:    uid,
		Name:   uid,
		Slot:   slot,
		Rarity: rarity,
		BaseStats: map[data.AffixType]float64{
			data.ATStrength: strength,
		},
	}
}
