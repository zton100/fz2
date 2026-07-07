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

	if decomposed.Count != 1 {
		t.Fatalf("decomposed count = %d, want 1", decomposed.Count)
	}
	if player.Materials[data.MatBase] != data.DecomposeBaseYield[data.RarityRare] {
		t.Fatalf("base materials = %d, want rare yield", player.Materials[data.MatBase])
	}
	if len(player.EquipBag) != 2 || player.EquipBag[0] != strong || player.EquipBag[1] != nil {
		t.Fatalf("bag = %v, want strong item and nil retained", player.EquipBag)
	}
}

func TestAutoDecomposeWeakBagReportsUpgradeRefund(t *testing.T) {
	player := model.NewPlayer("strategy")
	current := strategyEquipment("current", data.SlotWeapon, data.RarityCommon, 100)
	weak := strategyEquipment("weak", data.SlotWeapon, data.RarityCommon, 5)
	weak.Upgrade = 2
	player.Equipped[data.SlotWeapon] = current
	player.EquipBag = []*model.Equipment{weak}

	decomposed := autoDecomposeWeakBag(player)

	if decomposed.Count != 1 {
		t.Fatalf("decomposed count = %d, want 1", decomposed.Count)
	}
	if decomposed.UpgradeRefund <= 0 {
		t.Fatalf("upgrade refund = %d, want positive", decomposed.UpgradeRefund)
	}
}

func TestAutoTransferUpgradeReplacementsEquipsMatchedUpgrade(t *testing.T) {
	player := model.NewPlayer("strategy")
	current := strategyEquipment("current", data.SlotWeapon, data.RarityCommon, 50)
	current.Upgrade = 5
	candidate := strategyEquipment("candidate", data.SlotWeapon, data.RarityCommon, 55)
	player.Equipped[data.SlotWeapon] = current
	player.EquipBag = []*model.Equipment{candidate}

	transfers, gain := autoTransferUpgradeReplacements(player)

	if transfers != 1 {
		t.Fatalf("transfers = %d, want 1", transfers)
	}
	if gain <= 0 {
		t.Fatalf("transfer gain = %.1f, want positive", gain)
	}
	if player.Equipped[data.SlotWeapon] != candidate {
		t.Fatalf("equipped weapon = %v, want candidate", player.Equipped[data.SlotWeapon])
	}
	if candidate.Upgrade != 5 {
		t.Fatalf("candidate upgrade = %d, want inherited +5", candidate.Upgrade)
	}
	if len(player.EquipBag) != 1 || player.EquipBag[0] != current || current.Upgrade != 0 {
		t.Fatalf("bag/current after transfer = %+v current +%d, want old current in bag at +0", player.EquipBag, current.Upgrade)
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

func TestPowerGainIfEquippedAtMatchedUpgradeRevealsUpgradeGap(t *testing.T) {
	player := model.NewPlayer("strategy")
	current := strategyEquipment("current", data.SlotWeapon, data.RarityCommon, 50)
	current.Upgrade = 5
	candidate := strategyEquipment("candidate", data.SlotWeapon, data.RarityCommon, 55)
	player.Equipped[data.SlotWeapon] = current

	if gain := powerGainIfEquipped(player, candidate); gain >= 0 {
		t.Fatalf("immediate gain = %.1f, want negative from upgrade gap", gain)
	}
	if gain := powerGainIfEquippedAtMatchedUpgrade(player, candidate); gain <= 0 {
		t.Fatalf("matched upgrade gain = %.1f, want positive", gain)
	}
	if candidate.Upgrade != 0 {
		t.Fatalf("candidate upgrade mutated to %d, want 0", candidate.Upgrade)
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
