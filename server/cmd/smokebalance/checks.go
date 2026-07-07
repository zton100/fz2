package main

import (
	"fmt"

	"equipment-idle-server/internal/data"
)

func checkEarlyCycle(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.Ticks < cfg.EarlyTicks.Min || metrics.Ticks > cfg.EarlyTicks.Max {
		failures = append(failures, fmt.Sprintf("ticks out of target range %d..%d, got %d",
			cfg.EarlyTicks.Min,
			cfg.EarlyTicks.Max,
			metrics.Ticks))
	}
	if metrics.FinalFloor < cfg.EarlyTargetFloor {
		failures = append(failures, fmt.Sprintf("final floor %d, want >= %d", metrics.FinalFloor, cfg.EarlyTargetFloor))
	}
	if metrics.LootCount < cfg.EarlyMinLoot {
		failures = append(failures, fmt.Sprintf("loot count %d, want >= %d before first reincarnation gate", metrics.LootCount, cfg.EarlyMinLoot))
	}
	if metrics.FinalPower < metrics.StartPower {
		failures = append(failures, fmt.Sprintf("final power %.1f below start power %.1f", metrics.FinalPower, metrics.StartPower))
	}
	return failures
}

func checkReincarnationSouls(actualSouls int, cycle int, cfg balanceConfig) []string {
	want := cycle * cfg.ReincarnationSoulsPerCycle
	if actualSouls != want {
		return []string{fmt.Sprintf("souls after reincarnation = %d, want %d", actualSouls, want)}
	}
	return nil
}

func checkLongRun(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.FinalFloor < cfg.LongTargetFloor {
		failures = append(failures, fmt.Sprintf("long run reached floor %d, want >= %d", metrics.FinalFloor, cfg.LongTargetFloor))
	}
	if metrics.Ticks < cfg.LongTicks.Min || metrics.Ticks > cfg.LongTicks.Max {
		failures = append(failures, fmt.Sprintf("floor %d ticks out of target range %d..%d, got %d",
			cfg.LongTargetFloor,
			cfg.LongTicks.Min,
			cfg.LongTicks.Max,
			metrics.Ticks))
	}
	if metrics.BossReward < cfg.LongMinBossReward {
		failures = append(failures, fmt.Sprintf("long run boss reward %d, want >= %d", metrics.BossReward, cfg.LongMinBossReward))
	}
	return failures
}

func checkDeepRun(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.FinalFloor < cfg.DeepTargetFloor {
		failures = append(failures, fmt.Sprintf("deep run reached floor %d, want >= %d", metrics.FinalFloor, cfg.DeepTargetFloor))
	}
	if metrics.Ticks < cfg.DeepTicks.Min || metrics.Ticks > cfg.DeepTicks.Max {
		failures = append(failures, fmt.Sprintf("floor %d -> %d ticks out of target range %d..%d, got %d",
			cfg.DeepStartFloor,
			cfg.DeepTargetFloor,
			cfg.DeepTicks.Min,
			cfg.DeepTicks.Max,
			metrics.Ticks))
	}
	if cfg.RequireDeepSpecialAffix && metrics.AffixCategoryCounts[data.AffixSpecial] == 0 {
		failures = append(failures, fmt.Sprintf("floor %d -> %d saw no special affixes after unlock floor %d",
			cfg.DeepStartFloor,
			cfg.DeepTargetFloor,
			data.FloorUnlockSpecial))
	}
	return failures
}

func checkSecondLoop(metrics cycleMetrics, cfg balanceConfig, firstLoopStartPower float64, firstLoopTicksToDeepTarget int, spentDamage int) []string {
	var failures []string
	if cfg.RequireSecondLoopDamageTalent && spentDamage == 0 {
		failures = append(failures, "second loop did not spend reincarnation souls on damage talent")
	}
	if metrics.StartPower <= firstLoopStartPower {
		failures = append(failures, fmt.Sprintf("second loop start power %.1f should exceed first loop start power %.1f",
			metrics.StartPower,
			firstLoopStartPower))
	}
	if metrics.FinalFloor < cfg.SecondLoopTargetFloor {
		failures = append(failures, fmt.Sprintf("second loop reached floor %d, want >= %d", metrics.FinalFloor, cfg.SecondLoopTargetFloor))
	}
	if metrics.Ticks > firstLoopTicksToDeepTarget {
		failures = append(failures, fmt.Sprintf("second loop ticks %d should not exceed first loop ticks %d after damage talent",
			metrics.Ticks,
			firstLoopTicksToDeepTarget))
	}
	return failures
}

func printFailures(failures []string) {
	for _, failure := range failures {
		fmt.Printf("  FAIL: %s\n", failure)
	}
}
