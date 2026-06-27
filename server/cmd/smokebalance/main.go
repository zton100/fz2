package main

import (
	"fmt"
	"math/rand"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

func main() {
	rng := rand.New(rand.NewSource(time.Now().UnixNano()))
	gen := loot.NewGenerator(rng)
	drop := loot.NewDropTable(gen)

	fmt.Println("=== Balance Simulation ===")
	fmt.Println()

	for cycle := 1; cycle <= 3; cycle++ {
		player := model.NewPlayer(fmt.Sprintf("sim%d", cycle))
		player.Materials[data.MatBase] = 10

		// Give starter magic gear for all 8 slots
		for _, slot := range data.AllSlots() {
			eq := gen.Generate(slot, data.RarityMagic, player.Floor)
			player.Equipped[slot] = eq
		}

		power := reincarnation.ComputePlayerPower(player)
		fmt.Printf("--- Cycle %d: Start (power=%.1f, floor=%d) ---\n", cycle, power, player.Floor)
		ticks, floor := runCycle(player, gen, drop)
		power = reincarnation.ComputePlayerPower(player)
		fmt.Printf("Ticks to floor 10: %d (final floor=%d, power=%.1f)\n", ticks, floor, power)

		if ticks < 3 { fmt.Println("  WARN: too few ticks")
		} else if ticks > 5000 { fmt.Println("  WARN: too many ticks")
		} else { fmt.Println("  PASS: tick count in range") }

		if cycle < 3 {
			reincarnation.Reincarnate(player)
			player.Materials[data.MatBase] = 10
			for _, slot := range data.AllSlots() {
				eq := gen.Generate(slot, data.RarityMagic, player.Floor)
				player.Equipped[slot] = eq
			}
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
		if p.Floor > finalFloor { finalFloor = p.Floor }
		if p.Floor >= 10 { return ticks + 1, finalFloor }
	}
	return maxTicks, finalFloor
}

func autoEquipBest(p *model.Player) {
	if len(p.EquipBag) == 0 { return }
	best := map[data.Slot]*model.Equipment{}
	for _, eq := range p.EquipBag {
		if existing, ok := best[eq.Slot]; !ok || affixSum(eq) > affixSum(existing) {
			best[eq.Slot] = eq
		}
	}
	for slot, eq := range best {
		p.Equipped[slot] = eq
		for i, b := range p.EquipBag {
			if b.UID == eq.UID {
				p.EquipBag = append(p.EquipBag[:i], p.EquipBag[i+1:]...)
				break
			}
		}
	}
}

func affixSum(eq *model.Equipment) float64 {
	s := 0.0
	for _, a := range eq.Affixes { s += a.Value }
	for _, v := range eq.BaseStats { s += v }
	return s
}
