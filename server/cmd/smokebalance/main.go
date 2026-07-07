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
	TargetFloor         int
	Ticks               int
	FinalFloor          int
	StartPower          float64
	FinalPower          float64
	LootCount           int
	EquippedCount       int
	PowerGain           float64
	BossReward          int
	UpgradeCount        int
	DecomposeCount      int
	BaseMaterials       int
	RarityCounts        map[data.Rarity]int
	AffixCategoryCounts map[data.AffixCategory]int
	ReincarnSouls       int
	ReincarnErr         error
	PostReincPower      float64
}

func main() {
	cfg := defaultBalanceConfig()
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
		metrics := runCycle(player, drop, cfg.EarlyTargetFloor)
		totalEquipped += metrics.EquippedCount
		fmt.Printf("--- Cycle %d ---\n", cycle)
		fmt.Printf("Start: floor=1 power=%.1f souls=%d\n", metrics.StartPower, player.Souls)
		printMetrics(metrics)
		printLootMix(metrics)

		cycleFailures := checkEarlyCycle(metrics, cfg)
		if len(cycleFailures) > 0 {
			printFailures(cycleFailures)
			failed = true
		} else {
			fmt.Println("  PASS: tick count in target range")
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
			if soulFailures := checkReincarnationSouls(player.Souls, cycle, cfg); len(soulFailures) > 0 {
				printFailures(soulFailures)
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
	longMetrics := runCycle(longPlayer, longDrop, cfg.LongTargetFloor)
	fmt.Printf("--- Long Run: floor 1 -> %d ---\n", cfg.LongTargetFloor)
	fmt.Printf("Start: floor=1 power=%.1f souls=%d\n", longMetrics.StartPower, longPlayer.Souls)
	printMetrics(longMetrics)
	printLootMix(longMetrics)
	longFailures := checkLongRun(longMetrics, cfg)
	if len(longFailures) > 0 {
		printFailures(longFailures)
		failed = true
	} else {
		fmt.Printf("  PASS: floor %d tick count in target range\n", cfg.LongTargetFloor)
	}
	fmt.Println()

	deepMetrics := runCycle(longPlayer, longDrop, cfg.DeepTargetFloor)
	fmt.Printf("--- Deep Run: floor %d -> %d ---\n", cfg.DeepStartFloor, cfg.DeepTargetFloor)
	fmt.Printf("Start: floor=%d power=%.1f souls=%d\n", cfg.DeepStartFloor, deepMetrics.StartPower, longPlayer.Souls)
	printMetrics(deepMetrics)
	printLootMix(deepMetrics)
	deepFailures := checkDeepRun(deepMetrics, cfg)
	if len(deepFailures) > 0 {
		printFailures(deepFailures)
		failed = true
	} else {
		fmt.Printf("  PASS: floor %d -> %d tick count in target range\n", cfg.DeepStartFloor, cfg.DeepTargetFloor)
	}
	fmt.Println()

	lateMetrics := runCycle(longPlayer, longDrop, cfg.LateTargetFloor)
	fmt.Printf("--- Late Run: floor %d -> %d ---\n", cfg.LateStartFloor, cfg.LateTargetFloor)
	fmt.Printf("Start: floor=%d power=%.1f souls=%d\n", cfg.LateStartFloor, lateMetrics.StartPower, longPlayer.Souls)
	printMetrics(lateMetrics)
	printLootMix(lateMetrics)
	lateFailures := checkLateRun(lateMetrics, cfg)
	if len(lateFailures) > 0 {
		printFailures(lateFailures)
		failed = true
	} else {
		fmt.Printf("  PASS: floor %d -> %d tick count in target range\n", cfg.LateStartFloor, cfg.LateTargetFloor)
	}
	fmt.Printf("  PASS: floor %d -> %d artifact drops=%d\n",
		cfg.LateStartFloor,
		cfg.LateTargetFloor,
		lateMetrics.RarityCounts[data.RarityArtifact])
	fmt.Println()

	if err := reincarnation.Reincarnate(longPlayer); err != nil {
		fmt.Printf("FAIL: long run reincarnation failed: %v\n", err)
		failed = true
	} else {
		soulsAfterReincarn := longPlayer.Souls
		spentDamage := spendTalent(longPlayer, "damage")
		starter.GrantLoadout(longPlayer, longGen)
		secondMetrics := runCycle(longPlayer, longDrop, cfg.SecondLoopTargetFloor)
		fmt.Printf("--- Second Loop After Reincarnation: floor 1 -> %d ---\n", cfg.SecondLoopTargetFloor)
		fmt.Printf("Start: floor=1 power=%.1f souls=%d damage_talent=%d spent_damage=%d\n",
			secondMetrics.StartPower,
			soulsAfterReincarn,
			longPlayer.Talents["damage"],
			spentDamage)
		printMetrics(secondMetrics)
		printLootMix(secondMetrics)
		firstLoopTicksToSecondTarget := longMetrics.Ticks + deepMetrics.Ticks + lateMetrics.Ticks
		secondLoopFailures := checkSecondLoop(secondMetrics, cfg, longMetrics.StartPower, firstLoopTicksToSecondTarget, spentDamage)
		if len(secondLoopFailures) > 0 {
			printFailures(secondLoopFailures)
			failed = true
		} else {
			fmt.Printf("  PASS: second loop reached floor %d in %d ticks (first loop %d)\n",
				cfg.SecondLoopTargetFloor,
				secondMetrics.Ticks,
				firstLoopTicksToSecondTarget)
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

func printLootMix(metrics cycleMetrics) {
	fmt.Printf("Rarity mix: common=%d magic=%d rare=%d legendary=%d artifact=%d\n",
		metrics.RarityCounts[data.RarityCommon],
		metrics.RarityCounts[data.RarityMagic],
		metrics.RarityCounts[data.RarityRare],
		metrics.RarityCounts[data.RarityLegendary],
		metrics.RarityCounts[data.RarityArtifact])
	fmt.Printf("Affix mix: basic=%d derived=%d special=%d\n",
		metrics.AffixCategoryCounts[data.AffixBasic],
		metrics.AffixCategoryCounts[data.AffixDerived],
		metrics.AffixCategoryCounts[data.AffixSpecial])
}

func runCycle(p *model.Player, drop *loot.DropTable, targetFloor int) cycleMetrics {
	const maxTicks = 10000
	metrics := cycleMetrics{
		TargetFloor:         targetFloor,
		StartPower:          reincarnation.ComputePlayerPower(p),
		FinalFloor:          p.Floor,
		RarityCounts:        map[data.Rarity]int{},
		AffixCategoryCounts: map[data.AffixCategory]int{},
	}
	runner := dungeon.NewRunner(p, nil, drop)
	runner.LootCallback = func(eq *model.Equipment) {
		metrics.LootCount++
		metrics.RarityCounts[eq.Rarity]++
		for _, affix := range eq.Affixes {
			metrics.AffixCategoryCounts[data.AffixCategoryOf(affix.Type)]++
		}
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
