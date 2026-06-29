package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
	"equipment-idle-server/internal/starter"
)

func main() {
	rng := rand.New(rand.NewSource(42))
	gen := loot.NewGenerator(rng)
	drop := loot.NewDropTable(gen)
	player := model.NewPlayer("sim")
	starter.GrantLoadout(player, gen)

	fmt.Println("=== Balance Simulation ===")
	fmt.Println("seed: 42")
	fmt.Println()

	for cycle := 1; cycle <= 3; cycle++ {
		power := reincarnation.ComputePlayerPower(player)
		fmt.Printf("--- Cycle %d: Start (power=%.1f, floor=%d) ---\n", cycle, power, player.Floor)
		ticks, floor := runCycle(player, gen, drop)
		power = reincarnation.ComputePlayerPower(player)
		fmt.Printf("Ticks to floor 10: %d (final floor=%d, power=%.1f)\n", ticks, floor, power)

		if ticks < 3 {
			fmt.Println("  WARN: too few ticks")
		} else if ticks > 5000 {
			fmt.Println("  WARN: too many ticks")
		} else {
			fmt.Println("  PASS: tick count in range")
		}

		if cycle < 3 {
			reincarnation.Reincarnate(player)
			starter.GrantLoadout(player, gen)
			fmt.Printf("  Reincarnated: souls=%d maxFloor=%d\n", player.Souls, player.MaxFloor)
		}
		fmt.Println()
	}
	fmt.Println("=== Simulation Complete ===")
}

func runCycle(p *model.Player, gen *loot.Generator, drop *loot.DropTable) (ticks int, finalFloor int) {
	const maxTicks = 10000
	runner := dungeon.NewRunner(p, nil, drop)
	for ticks = 0; ticks < maxTicks; ticks++ {
		runner.Tick()
		autoEquipBest(p)
		if p.Floor > finalFloor {
			finalFloor = p.Floor
		}
		if p.Floor >= 10 {
			return ticks + 1, finalFloor
		}
	}
	return maxTicks, finalFloor
}

func autoEquipBest(p *model.Player) {
	if len(p.EquipBag) == 0 {
		return
	}
	for _, eq := range p.EquipBag {
		if improvesPower(p, eq) {
			p.Equipped[eq.Slot] = eq
		}
	}
	p.EquipBag = p.EquipBag[:0]
}

func improvesPower(p *model.Player, eq *model.Equipment) bool {
	currentPower := reincarnation.ComputePlayerPower(p)
	old := p.Equipped[eq.Slot]
	p.Equipped[eq.Slot] = eq
	nextPower := reincarnation.ComputePlayerPower(p)
	if old == nil {
		delete(p.Equipped, eq.Slot)
	} else {
		p.Equipped[eq.Slot] = old
	}
	return nextPower > currentPower
}
