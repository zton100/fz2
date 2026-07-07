package main

import (
	"math/rand"

	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
	"equipment-idle-server/internal/upgrade"
)

func autoEquipBest(p *model.Player) (equipped int, powerGain float64) {
	if len(p.EquipBag) == 0 {
		return 0, 0
	}
	kept := p.EquipBag[:0]
	for _, eq := range p.EquipBag {
		if eq == nil {
			continue
		}
		if gain := powerGainIfEquipped(p, eq); gain > 0 {
			p.Equipped[eq.Slot] = eq
			equipped++
			powerGain += gain
			continue
		}
		kept = append(kept, eq)
	}
	p.EquipBag = kept
	return equipped, powerGain
}

func autoDecomposeWeakBag(p *model.Player) int {
	kept := p.EquipBag[:0]
	decomposed := 0
	for _, eq := range p.EquipBag {
		if eq == nil || powerGainIfEquipped(p, eq) > 0 {
			kept = append(kept, eq)
			continue
		}
		if _, err := crafting.Decompose(p, eq); err == nil {
			decomposed++
		}
	}
	p.EquipBag = kept
	return decomposed
}

func autoUpgradeEquipped(p *model.Player, rng *rand.Rand) int {
	upgrades := 0
	for attempts := 0; attempts < 32; attempts++ {
		eq := bestAffordableUpgrade(p)
		if eq == nil {
			return upgrades
		}
		result, err := upgrade.Upgrade(p, rng, eq)
		if err != nil {
			return upgrades
		}
		if result.Success {
			upgrades++
		}
	}
	return upgrades
}

func bestAffordableUpgrade(p *model.Player) *model.Equipment {
	var best *model.Equipment
	bestEfficiency := 0.0
	for _, eq := range p.Equipped {
		if eq == nil || eq.Upgrade >= upgrade.MaxUpgrade {
			continue
		}
		targetLevel := eq.Upgrade + 1
		cost := data.UpgradeCostTable[targetLevel]
		if !p.HasMaterial(data.MatBase, cost) {
			continue
		}
		gain := powerGainIfUpgraded(p, eq)
		if gain <= 0 {
			continue
		}
		efficiency := gain / float64(cost)
		if best == nil || efficiency > bestEfficiency {
			best = eq
			bestEfficiency = efficiency
		}
	}
	return best
}

func powerGainIfUpgraded(p *model.Player, eq *model.Equipment) float64 {
	currentPower := reincarnation.ComputePlayerPower(p)
	eq.Upgrade++
	nextPower := reincarnation.ComputePlayerPower(p)
	eq.Upgrade--
	return nextPower - currentPower
}

func powerGainIfEquipped(p *model.Player, eq *model.Equipment) float64 {
	currentPower := reincarnation.ComputePlayerPower(p)
	old := p.Equipped[eq.Slot]
	p.Equipped[eq.Slot] = eq
	nextPower := reincarnation.ComputePlayerPower(p)
	if old == nil {
		delete(p.Equipped, eq.Slot)
	} else {
		p.Equipped[eq.Slot] = old
	}
	return nextPower - currentPower
}
