package main

import (
	"fmt"
	"math/rand"
	"os"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
	"equipment-idle-server/internal/starter"
)

type cycleMetrics struct {
	Ticks          int
	FinalFloor     int
	StartPower     float64
	FinalPower     float64
	LootCount      int
	EquippedCount  int
	PowerGain      float64
	BossReward     int
	RarityCounts   map[data.Rarity]int
	ReincarnSouls  int
	ReincarnErr    error
	PostReincPower float64
}

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
		metrics := runCycle(player, drop)
		totalEquipped += metrics.EquippedCount
		fmt.Printf("--- Cycle %d ---\n", cycle)
		fmt.Printf("Start: floor=1 power=%.1f souls=%d\n", metrics.StartPower, player.Souls)
		fmt.Printf("To floor 10: ticks=%d final_floor=%d loot=%d equipped=%d power=%.1f (gain %.1f)\n",
			metrics.Ticks, metrics.FinalFloor, metrics.LootCount, metrics.EquippedCount, metrics.FinalPower, metrics.PowerGain)
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

func runCycle(p *model.Player, drop *loot.DropTable) cycleMetrics {
	const maxTicks = 10000
	metrics := cycleMetrics{
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
	for ticks := 0; ticks < maxTicks; ticks++ {
		runner.Tick()
		equipped, gain := autoEquipBest(p)
		metrics.EquippedCount += equipped
		metrics.PowerGain += gain
		if p.Floor > metrics.FinalFloor {
			metrics.FinalFloor = p.Floor
		}
		if p.Floor >= 10 {
			metrics.Ticks = ticks + 1
			metrics.FinalPower = reincarnation.ComputePlayerPower(p)
			return metrics
		}
	}
	metrics.Ticks = maxTicks
	metrics.FinalPower = reincarnation.ComputePlayerPower(p)
	return metrics
}

func autoEquipBest(p *model.Player) (equipped int, powerGain float64) {
	if len(p.EquipBag) == 0 {
		return 0, 0
	}
	for _, eq := range p.EquipBag {
		if gain := powerGainIfEquipped(p, eq); gain > 0 {
			p.Equipped[eq.Slot] = eq
			equipped++
			powerGain += gain
		}
	}
	p.EquipBag = p.EquipBag[:0]
	return equipped, powerGain
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
