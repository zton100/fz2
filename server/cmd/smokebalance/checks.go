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

func checkLateRun(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.FinalFloor < cfg.LateTargetFloor {
		failures = append(failures, fmt.Sprintf("late run reached floor %d, want >= %d", metrics.FinalFloor, cfg.LateTargetFloor))
	}
	if metrics.Ticks < cfg.LateTicks.Min || metrics.Ticks > cfg.LateTicks.Max {
		failures = append(failures, fmt.Sprintf("floor %d -> %d ticks out of target range %d..%d, got %d",
			cfg.LateStartFloor,
			cfg.LateTargetFloor,
			cfg.LateTicks.Min,
			cfg.LateTicks.Max,
			metrics.Ticks))
	}
	if metrics.RarityCounts[data.RarityArtifact] < cfg.LateMinArtifactDrops {
		failures = append(failures, fmt.Sprintf("floor %d -> %d artifact drops %d, want >= %d",
			cfg.LateStartFloor,
			cfg.LateTargetFloor,
			metrics.RarityCounts[data.RarityArtifact],
			cfg.LateMinArtifactDrops))
	}
	return failures
}

func checkEndgameRun(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.FinalFloor < cfg.EndgameTargetFloor {
		failures = append(failures, fmt.Sprintf("endgame run reached floor %d, want >= %d", metrics.FinalFloor, cfg.EndgameTargetFloor))
	}
	if metrics.Ticks < cfg.EndgameTicks.Min || metrics.Ticks > cfg.EndgameTicks.Max {
		failures = append(failures, fmt.Sprintf("floor %d -> %d ticks out of target range %d..%d, got %d",
			cfg.EndgameStartFloor,
			cfg.EndgameTargetFloor,
			cfg.EndgameTicks.Min,
			cfg.EndgameTicks.Max,
			metrics.Ticks))
	}
	if metrics.RarityCounts[data.RarityArtifact] < cfg.EndgameMinArtifactDrops {
		failures = append(failures, fmt.Sprintf("floor %d -> %d artifact drops %d, want >= %d",
			cfg.EndgameStartFloor,
			cfg.EndgameTargetFloor,
			metrics.RarityCounts[data.RarityArtifact],
			cfg.EndgameMinArtifactDrops))
	}
	return failures
}

func checkFrontierRun(metrics cycleMetrics, cfg balanceConfig) []string {
	var failures []string
	if metrics.FinalFloor < cfg.FrontierTargetFloor {
		failures = append(failures, fmt.Sprintf("frontier run reached floor %d, want >= %d", metrics.FinalFloor, cfg.FrontierTargetFloor))
	}
	if metrics.Ticks < cfg.FrontierTicks.Min || metrics.Ticks > cfg.FrontierTicks.Max {
		failures = append(failures, fmt.Sprintf("floor %d -> %d ticks out of target range %d..%d, got %d",
			cfg.FrontierStartFloor,
			cfg.FrontierTargetFloor,
			cfg.FrontierTicks.Min,
			cfg.FrontierTicks.Max,
			metrics.Ticks))
	}
	if metrics.RarityCounts[data.RarityArtifact] < cfg.FrontierMinArtifactDrops {
		failures = append(failures, fmt.Sprintf("floor %d -> %d artifact drops %d, want >= %d",
			cfg.FrontierStartFloor,
			cfg.FrontierTargetFloor,
			metrics.RarityCounts[data.RarityArtifact],
			cfg.FrontierMinArtifactDrops))
	}
	return failures
}

func checkArtifactDistribution(metrics artifactDistributionMetrics, cfg balanceConfig) []string {
	var failures []string
	failures = append(failures, checkArtifactSegmentDistribution(metrics.Endgame, cfg.EndgameArtifactDistribution)...)
	failures = append(failures, checkArtifactSegmentDistribution(metrics.Frontier, cfg.FrontierArtifactDistribution)...)
	return failures
}

func checkArtifactSegmentDistribution(metrics artifactSegmentDistribution, target artifactDistributionTarget) []string {
	var failures []string
	if metrics.Total < target.MinTotal {
		failures = append(failures, fmt.Sprintf("%s artifact total %d across %d seeds, want >= %d",
			metrics.Name,
			metrics.Total,
			metrics.SeedCount,
			target.MinTotal))
	}
	if metrics.SeedsWithDrop < target.MinSeedsWithDrop {
		failures = append(failures, fmt.Sprintf("%s artifact seeds_with_drop %d, want >= %d",
			metrics.Name,
			metrics.SeedsWithDrop,
			target.MinSeedsWithDrop))
	}
	return failures
}

func checkPostReincarnationLoop(metrics cycleMetrics, cfg balanceConfig, firstRunStartPower float64, firstRunTicksToTarget int, spentDamage int) []string {
	var failures []string
	if cfg.RequirePostReincarnationDamageTalent && spentDamage == 0 {
		failures = append(failures, "post-reincarnation loop did not spend reincarnation souls on damage talent")
	}
	if metrics.StartPower <= firstRunStartPower {
		failures = append(failures, fmt.Sprintf("post-reincarnation loop start power %.1f should exceed first run start power %.1f",
			metrics.StartPower,
			firstRunStartPower))
	}
	if metrics.FinalFloor < cfg.PostReincarnationTargetFloor {
		failures = append(failures, fmt.Sprintf("post-reincarnation loop reached floor %d, want >= %d", metrics.FinalFloor, cfg.PostReincarnationTargetFloor))
	}
	if metrics.Ticks > firstRunTicksToTarget {
		failures = append(failures, fmt.Sprintf("post-reincarnation loop ticks %d should not exceed first run ticks %d after damage talent",
			metrics.Ticks,
			firstRunTicksToTarget))
	}
	return failures
}

func printFailures(failures []string) {
	for _, failure := range failures {
		fmt.Printf("  FAIL: %s\n", failure)
	}
}
