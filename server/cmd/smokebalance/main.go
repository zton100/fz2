package main

import (
	"fmt"
	"math/rand"
	"os"

	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
	"equipment-idle-server/internal/starter"
	"equipment-idle-server/internal/upgrade"
)

type cycleMetrics struct {
	TargetFloor    int
	Ticks          int
	FinalFloor     int
	StartPower     float64
	FinalPower     float64
	LootCount      int
	EquippedCount  int
	PowerGain      float64
	BossReward     int
	UpgradeCount   int
	DecomposeCount int
	BaseMaterials  int
	RarityCounts   map[data.Rarity]int
	ReincarnSouls  int
	ReincarnErr    error
	PostReincPower float64
}

const longTargetFloor = 30

func main() {
	rng := rand.New(rand.NewSource(42))
	gen := loot.NewGenerator(rng)
	drop := loot.NewDropTable(gen)
	player := model.NewPlayer("sim")
	starter.GrantLoadout(player, gen)

	fmt.Println("=== Balance Simulation ===")
	fmt.Println("seed: 42")
	fmt.Println()

	failed := false
	totalEquipped := 0
	for cycle := 1; cycle <= 3; cycle++ {
		metrics := runCycle(player, drop, 10)
		totalEquipped += metrics.EquippedCount
		fmt.Printf("--- Cycle %d ---\n", cycle)
		fmt.Printf("Start: floor=1 power=%.1f souls=%d\n", metrics.StartPower, player.Souls)
		printMetrics(metrics)
		fmt.Printf("Rarity mix: common=%d magic=%d rare=%d legendary=%d artifact=%d\n",
			metrics.RarityCounts[data.RarityCommon],
			metrics.RarityCounts[data.RarityMagic],
			metrics.RarityCounts[data.RarityRare],
			metrics.RarityCounts[data.RarityLegendary],
			metrics.RarityCounts[data.RarityArtifact])

		if metrics.Ticks < 5 || metrics.Ticks > 25 {
			fmt.Printf("  FAIL: ticks out of target range 5..25, got %d\n", metrics.Ticks)
			failed = true
		} else {
			fmt.Println("  PASS: tick count in target range")
		}
		if metrics.FinalFloor < 10 {
			fmt.Printf("  FAIL: final floor %d, want >= 10\n", metrics.FinalFloor)
			failed = true
		}
		if metrics.LootCount < 8 {
			fmt.Printf("  FAIL: loot count %d, want >= 8 before first reincarnation gate\n", metrics.LootCount)
			failed = true
		}
		if metrics.FinalPower < metrics.StartPower {
			fmt.Printf("  FAIL: final power %.1f below start power %.1f\n", metrics.FinalPower, metrics.StartPower)
			failed = true
		}

		if cycle < 3 {
			metrics.ReincarnErr = reincarnation.Reincarnate(player)
			if metrics.ReincarnErr != nil {
				fmt.Printf("  FAIL: reincarn failed: %v\n", metrics.ReincarnErr)
				failed = true
			}
			starter.GrantLoadout(player, gen)
			metrics.ReincarnSouls = player.Souls
			metrics.PostReincPower = reincarnation.ComputePlayerPower(player)
			fmt.Printf("Reincarnated: souls=%d max_floor=%d reset_power=%.1f\n",
				metrics.ReincarnSouls, player.MaxFloor, metrics.PostReincPower)
			if player.Souls != cycle*2 {
				fmt.Printf("  FAIL: souls after reincarnation = %d, want %d\n", player.Souls, cycle*2)
				failed = true
			}
		}
		fmt.Println()
	}

	longRNG := rand.New(rand.NewSource(84))
	longGen := loot.NewGenerator(longRNG)
	longDrop := loot.NewDropTable(longGen)
	longPlayer := model.NewPlayer("long-sim")
	starter.GrantLoadout(longPlayer, longGen)
	longMetrics := runCycle(longPlayer, longDrop, longTargetFloor)
	fmt.Printf("--- Long Run: floor 1 -> %d ---\n", longTargetFloor)
	fmt.Printf("Start: floor=1 power=%.1f souls=%d\n", longMetrics.StartPower, longPlayer.Souls)
	printMetrics(longMetrics)
	fmt.Printf("Rarity mix: common=%d magic=%d rare=%d legendary=%d artifact=%d\n",
		longMetrics.RarityCounts[data.RarityCommon],
		longMetrics.RarityCounts[data.RarityMagic],
		longMetrics.RarityCounts[data.RarityRare],
		longMetrics.RarityCounts[data.RarityLegendary],
		longMetrics.RarityCounts[data.RarityArtifact])
	if longMetrics.FinalFloor < longTargetFloor {
		fmt.Printf("  FAIL: long run reached floor %d, want >= %d\n", longMetrics.FinalFloor, longTargetFloor)
		failed = true
	}
	if longMetrics.Ticks < 25 || longMetrics.Ticks > 400 {
		fmt.Printf("  FAIL: floor %d ticks out of target range 25..400, got %d\n", longTargetFloor, longMetrics.Ticks)
		failed = true
	} else {
		fmt.Printf("  PASS: floor %d tick count in target range\n", longTargetFloor)
	}
	if longMetrics.BossReward < 180 {
		fmt.Printf("  FAIL: long run boss reward %d, want >= 180\n", longMetrics.BossReward)
		failed = true
	}
	fmt.Println()

	if err := reincarnation.Reincarnate(longPlayer); err != nil {
		fmt.Printf("FAIL: long run reincarnation failed: %v\n", err)
		failed = true
	} else {
		soulsAfterReincarn := longPlayer.Souls
		spentDamage := spendTalent(longPlayer, "damage")
		starter.GrantLoadout(longPlayer, longGen)
		secondMetrics := runCycle(longPlayer, longDrop, longTargetFloor)
		fmt.Printf("--- Second Loop After Reincarnation: floor 1 -> %d ---\n", longTargetFloor)
		fmt.Printf("Start: floor=1 power=%.1f souls=%d damage_talent=%d spent_damage=%d\n",
			secondMetrics.StartPower,
			soulsAfterReincarn,
			longPlayer.Talents["damage"],
			spentDamage)
		printMetrics(secondMetrics)
		fmt.Printf("Rarity mix: common=%d magic=%d rare=%d legendary=%d artifact=%d\n",
			secondMetrics.RarityCounts[data.RarityCommon],
			secondMetrics.RarityCounts[data.RarityMagic],
			secondMetrics.RarityCounts[data.RarityRare],
			secondMetrics.RarityCounts[data.RarityLegendary],
			secondMetrics.RarityCounts[data.RarityArtifact])
		if spentDamage == 0 {
			fmt.Println("  FAIL: second loop did not spend reincarnation souls on damage talent")
			failed = true
		}
		if secondMetrics.StartPower <= longMetrics.StartPower {
			fmt.Printf("  FAIL: second loop start power %.1f should exceed first loop start power %.1f\n",
				secondMetrics.StartPower,
				longMetrics.StartPower)
			failed = true
		}
		if secondMetrics.FinalFloor < longTargetFloor {
			fmt.Printf("  FAIL: second loop reached floor %d, want >= %d\n", secondMetrics.FinalFloor, longTargetFloor)
			failed = true
		}
		if secondMetrics.Ticks > longMetrics.Ticks {
			fmt.Printf("  FAIL: second loop ticks %d should not exceed first loop ticks %d after damage talent\n",
				secondMetrics.Ticks,
				longMetrics.Ticks)
			failed = true
		} else {
			fmt.Printf("  PASS: second loop reached floor %d in %d ticks (first loop %d)\n",
				longTargetFloor,
				secondMetrics.Ticks,
				longMetrics.Ticks)
		}
		fmt.Println()
	}

	if totalEquipped == 0 {
		fmt.Println("FAIL: no dropped item improved equipped power across all cycles")
		failed = true
	}
	if failed {
		fmt.Println("=== Simulation Failed ===")
		os.Exit(1)
	}
	fmt.Println("=== Simulation Complete ===")
}

func spendTalent(p *model.Player, name string) int {
	spent := 0
	for {
		if err := reincarnation.UpgradeTalent(p, name); err != nil {
			return spent
		}
		spent++
	}
}

func printMetrics(metrics cycleMetrics) {
	fmt.Printf("To floor %d: ticks=%d final_floor=%d loot=%d equipped=%d power=%.1f (gain %.1f) boss_reward=%d\n",
		metrics.TargetFloor,
		metrics.Ticks,
		metrics.FinalFloor,
		metrics.LootCount,
		metrics.EquippedCount,
		metrics.FinalPower,
		metrics.PowerGain,
		metrics.BossReward)
	fmt.Printf("Crafting: upgrades=%d decomposed=%d base_materials=%d\n",
		metrics.UpgradeCount,
		metrics.DecomposeCount,
		metrics.BaseMaterials)
}

func runCycle(p *model.Player, drop *loot.DropTable, targetFloor int) cycleMetrics {
	const maxTicks = 10000
	metrics := cycleMetrics{
		TargetFloor:  targetFloor,
		StartPower:   reincarnation.ComputePlayerPower(p),
		FinalFloor:   p.Floor,
		RarityCounts: map[data.Rarity]int{},
	}
	runner := dungeon.NewRunner(p, nil, drop)
	runner.LootCallback = func(eq *model.Equipment) {
		metrics.LootCount++
		metrics.RarityCounts[eq.Rarity]++
	}
	runner.BossRewardCallback = func(floor int, amount int) {
		metrics.BossReward += amount
	}
	craftRNG := rand.New(rand.NewSource(9001))
	for ticks := 0; ticks < maxTicks; ticks++ {
		beforeFloor := p.Floor
		runner.Tick()
		equipped, gain := autoEquipBest(p)
		metrics.EquippedCount += equipped
		metrics.PowerGain += gain
		if p.Floor == beforeFloor {
			metrics.DecomposeCount += autoDecomposeWeakBag(p)
			metrics.UpgradeCount += autoUpgradeEquipped(p, craftRNG)
		}
		if p.Floor > metrics.FinalFloor {
			metrics.FinalFloor = p.Floor
		}
		if p.Floor >= targetFloor {
			metrics.Ticks = ticks + 1
			metrics.FinalPower = reincarnation.ComputePlayerPower(p)
			metrics.BaseMaterials = p.Materials[data.MatBase]
			return metrics
		}
	}
	metrics.Ticks = maxTicks
	metrics.FinalPower = reincarnation.ComputePlayerPower(p)
	metrics.BaseMaterials = p.Materials[data.MatBase]
	return metrics
}

func autoEquipBest(p *model.Player) (equipped int, powerGain float64) {
	if len(p.EquipBag) == 0 {
		return 0, 0
	}
	kept := p.EquipBag[:0]
	for _, eq := range p.EquipBag {
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
